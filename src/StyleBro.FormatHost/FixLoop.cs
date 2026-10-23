using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;

namespace StyleBro.Host;

/// <summary>
/// The host's check and fix runs (docs/proposals/format-speed.md, phase 2): dotnet format's formatters in its order, its own whitespace,
/// line ending, charset and import formatters as they are, and in place of its two analyzer formatters (code style, analyzer
/// references) the same steps with three changes: the ids in a fixed order (<see cref="FixOrder"/>), each fixer called once
/// per id, and the analysis before each id from <see cref="AnalysisCache"/>, so only the projects a fix changed (and the ones
/// referencing them) are analyzed again. What it reports, which analyzers run, which diagnostics count and how a Fix All is
/// applied stay dotnet format's (its FilterAnalyzersAsync, CodeAnalysisResult and SolutionCodeFixApplier, by reflection).
/// A check (--verify-no-changes) runs the same formatters and the analyzer formatters' first analysis, as dotnet format does,
/// from the same cache: the fix run after it, and the check after a run that changed files, analyze again only the projects
/// whose content changed since.
/// </summary>
internal sealed class FixLoop(DotnetFormat format, AnalysisCache cache, Func<string, IDisposable> measure)
{
    /// <summary>
    /// Runs every formatter of <paramref name="options"/>' categories on <paramref name="documents"/> and returns the fixed solution
    /// (CodeFormatter.RunCodeFormattersAsync); a check's formatters only add what they would change to <paramref name="files"/>.
    /// </summary>
    public async Task<Solution> RunAsync(Workspace workspace, Solution solution, ImmutableArray<DocumentId> documents, object options, ILogger logger, object files, CancellationToken cancellation)
    {
        var categories = Convert.ToInt32(DotnetFormat.Option<object>(options, "FixCategory"));
        foreach (var (formatter, category, analyzers) in format.Formatters())
        {
            if ((categories & category) != category)
            {
                continue;
            }

            using var phase = measure($"host: {(DotnetFormat.Option<bool>(options, "SaveFormattedFiles") ? "fix" : "check")} {(analyzers?.Name ?? formatter.GetType().Name).ToLowerInvariant()}");
            solution = analyzers is null
                ? await format.RunFormatterAsync(formatter, workspace, solution, documents, options, logger, files, cancellation).ConfigureAwait(false)
                : await this.AnalyzersAsync(analyzers, workspace, solution, documents, options, logger, files, cancellation).ConfigureAwait(false);
        }

        return solution;
    }

    // AnalyzerFormatter.FormatAsync: a check reports the first analysis, a fix run then fixes id by id.
    private async Task<Solution> AnalyzersAsync(DotnetFormat.AnalyzerParts parts, Workspace workspace, Solution solution, ImmutableArray<DocumentId> documents, object options, ILogger logger, object files, CancellationToken cancellation)
    {
        var (projectAnalyzersAndFixers, raw) = format.AnalyzersAndFixers(parts, workspace, solution, options, logger);
        if (projectAnalyzersAndFixers.Count == 0)
        {
            return solution;
        }

        var allFixers = projectAnalyzersAndFixers.Values.SelectMany(a => a.Fixers).ToImmutableArray();

        // Like dotnet format: the compiler's ids its fixers fix with Fix All in the solution (its intersection with --diagnostics
        // is computed and dropped there; CodeAnalysisResult leaves out ids --diagnostics doesn't name anyway).
        var compilerIds = parts.IncludeCompilerDiagnostics
            ? allFixers.Where(f => f.GetFixAllProvider()?.GetSupportedFixAllScopes().Contains(FixAllScope.Solution) == true)
                .SelectMany(f => f.FixableDiagnosticIds.Where(id => id.StartsWith("CS", StringComparison.Ordinal) || id.StartsWith("BC", StringComparison.Ordinal)))
                .ToImmutableHashSet()
            : [];
        var watch = Stopwatch.StartNew();
        logger.LogTrace(format.Text("Running_0_analysis"), parts.Name);
        var paths = documents.Select(d => solution.GetDocument(d)?.FilePath).OfType<string>().ToImmutableHashSet();
        logger.LogTrace(format.Text("Determining_diagnostics"));
        var severity = DotnetFormat.Severity(parts, options);
        var projectAnalyzers = await format.FilterAnalyzersAsync(solution, raw, paths, severity, options, cancellation).ConfigureAwait(false);

        // The first analysis: every project (of a project command: that one), all its analyzers.
        var workspaceType = Convert.ToInt32(DotnetFormat.Option<object>(options, "WorkspaceType"));
        var target = DotnetFormat.Option<string>(options, "WorkspaceFilePath");
        var found = new List<(Project Project, Diagnostic Diagnostic)>();
        foreach (var project in solution.Projects.Where(p => workspaceType == 2 || p.FilePath == target))
        {
            if (projectAnalyzers.TryGetValue(project.Id, out var analyzers) && !analyzers.IsEmpty)
            {
                found.AddRange((await this.AnalyzeAsync(analyzers, project, paths, severity, compilerIds, logger, cancellation).ConfigureAwait(false)).Select(d => (project, d)));
            }
        }

        var kept = found.Where(f => DotnetFormat.Accepts(options, f.Diagnostic.Id)).ToList();
        var level = DotnetFormat.Option<LogLevel>(options, "LogLevel");
        var save = DotnetFormat.Option<bool>(options, "SaveFormattedFiles");
        foreach (var (_, diagnostic) in kept)
        {
            if (solution.GetDocument(diagnostic.Location.SourceTree) is { } document)
            {
                var position = diagnostic.Location.GetMappedLineSpan().StartLinePosition;
                if (!save || level == LogLevel.Debug)
                {
                    format.LogDiagnostic(logger, document, position, diagnostic, DotnetFormat.Option<bool>(options, "ChangesAreErrors"));
                }

                format.AddFile(files, document, position, diagnostic.Id, $"{diagnostic.Severity.ToString().ToLowerInvariant()} {diagnostic.Id}: {diagnostic.GetMessage()}");
            }
        }

        var firstMs = watch.ElapsedMilliseconds;
        logger.LogTrace(format.Text("Complete_in_0_ms"), firstMs);
        if (!save)
        {
            // A check reports what it found and fixes nothing.
            logger.LogTrace(format.Text("Analysis_complete_in_0ms_"), watch.ElapsedMilliseconds);
            return solution;
        }

        logger.LogTrace(format.Text("Fixing_diagnostics"));
        var withIds = kept.GroupBy(k => k.Project.Id).ToDictionary(g => g.Key, g => g.Select(k => k.Diagnostic.Id).ToHashSet(StringComparer.Ordinal));
        foreach (var id in FixOrder.Of(kept.Select(k => k.Diagnostic.Id)))
        {
            var fixers = FixOrder.FixersFor(id, allFixers);
            if (fixers.Count == 0)
            {
                logger.LogWarning(format.Text("Unable_to_fix_0_No_associated_code_fix_found"), id);
                continue;
            }

            // The id's diagnostics in the projects that had it, on the solution as the fixes so far left it.
            var now = new List<(Project Project, Diagnostic Diagnostic)>();
            foreach (var project in solution.Projects)
            {
                if (withIds.TryGetValue(project.Id, out var ids) && ids.Contains(id))
                {
                    var analyzers = projectAnalyzers[project.Id].Where(a => a.SupportedDiagnostics.Any(d => d.Id == id)).ToImmutableArray();
                    now.AddRange((await this.AnalyzeAsync(analyzers, project, paths, severity, compilerIds, logger, cancellation).ConfigureAwait(false))
                        .Where(d => d.Id == id)
                        .Select(d => (project, d)));
                }
            }

            var idResult = format.AnalysisResult(options, now);
            if (!DotnetFormat.HasDiagnostics(idResult))
            {
                continue;
            }

            foreach (var fixer in fixers)
            {
                var fixedSolution = await DotnetFormat.ApplyAsync(parts, solution, idResult, fixer, id, logger, cancellation).ConfigureAwait(false);
                if (fixedSolution.GetChanges(solution).GetProjectChanges().Any(p => p.GetChangedDocuments().Any() || p.GetChangedAdditionalDocuments().Any()))
                {
                    solution = fixedSolution;
                }
            }
        }

        logger.LogTrace(format.Text("Complete_in_0_ms"), watch.ElapsedMilliseconds - firstMs);
        logger.LogTrace(format.Text("Analysis_complete_in_0ms_"), watch.ElapsedMilliseconds);
        return solution;
    }

    // AnalyzerRunner.RunCodeAnalysisAsync, its analysis from the cache.
    private async Task<List<Diagnostic>> AnalyzeAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, Project project, ImmutableHashSet<string> paths, DiagnosticSeverity severity, ImmutableHashSet<string> compilerIds, ILogger logger, CancellationToken cancellation)
    {
        if (analyzers.IsEmpty && compilerIds.IsEmpty)
        {
            return [];
        }

        if (!AllReferencesLoaded(project))
        {
            logger.LogWarning(format.Text("Required_references_did_not_load_for_0_or_referenced_project_Run_dotnet_restore_prior_to_formatting"), project.Name);
            return [];
        }

        var diagnostics = await cache.GetAsync(project, analyzers, compilerIds, n => logger.LogDebug(format.Text("Running_0_analyzers_on_1"), n, project.Name), cancellation).ConfigureAwait(false);
        return [.. diagnostics.Where(d => !d.IsSuppressed && d.Severity >= severity && d.Location.IsInSource && d.Location.SourceTree is { } tree && paths.Contains(tree.FilePath))];
    }

    private static bool AllReferencesLoaded(Project project) =>
        project.MetadataReferences.Any(r => r.Display?.EndsWith("mscorlib.dll", StringComparison.Ordinal) == true)
        && project.ProjectReferences.Select(r => project.Solution.GetProject(r.ProjectId)).All(p => p is not null && AllReferencesLoaded(p));
}
