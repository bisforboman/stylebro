using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Host;

/// <summary>
/// The diagnostics each analyzer reported on a project, kept for as long as the project's content stays as it was
/// (docs/format-engine.md, "What it reuses"). The key is the project and its versions: its own, and the newest of its
/// documents and of the projects it references (<see cref="Project.GetDependentVersionAsync"/>), which any text change in it
/// or in a project it references makes newer. So the results carry over to every solution with the same content: from a
/// framework's check to the fix run that follows it, between the views of two runs that share projects, from one id's
/// analysis to the next where the fix didn't reach the project, and to the check after a run that changed files, which
/// analyzes again only the projects a fix reached and the ones referencing them. Only the newest results of a project are
/// kept. An analyzer's diagnostics don't depend on which other analyzers run with it (no suppressor runs here: dotnet
/// format's per-id step leaves them out too), so each one is kept on its own; the compiler's diagnostics too.
/// </summary>
public sealed class AnalysisCache
{
    private readonly Dictionary<ProjectId, Results> results = [];

    /// <summary>How many times an analyzer (or the compiler's diagnostics) ran on a project (tests: what the cache saved).</summary>
    public int Runs { get; private set; }

    /// <summary>Diagnostics in a fixed order: by file, position, id and message.</summary>
    public static ImmutableArray<Diagnostic> Ordered(IEnumerable<Diagnostic> diagnostics) =>
        [.. diagnostics.OrderBy(d => d.Location.SourceTree?.FilePath ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(d => d.Location.SourceSpan.Start)
            .ThenBy(d => d.Location.SourceSpan.End)
            .ThenBy(d => d.Id, StringComparer.Ordinal)
            .ThenBy(d => d.GetMessage(), StringComparer.Ordinal)];

    /// <summary>
    /// The diagnostics <paramref name="analyzers"/> report on <paramref name="project"/> (pragma-suppressed ones left out, like
    /// CompilationWithAnalyzers.GetAnalyzerDiagnosticsAsync), in a fixed order: by file, position and id, and the compiler's
    /// diagnostics with the ids <paramref name="compilerIds"/> names. The project compiles only when something has to run;
    /// <paramref name="running"/> is called with the number of analyzers that have to, when any do.
    /// </summary>
    public async Task<ImmutableArray<Diagnostic>> GetAsync(Project project, ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableHashSet<string> compilerIds, Action<int>? running, CancellationToken cancellation)
    {
        var key = (project.Version, await project.GetDependentVersionAsync(cancellation).ConfigureAwait(false), project.AnalyzerOptions);
        var compilerKey = compilerIds.IsEmpty ? null : string.Join(";", compilerIds.Order(StringComparer.Ordinal));
        Results cached;
        List<DiagnosticAnalyzer> missing;
        bool compilerMissing;
        lock (this.results)
        {
            if (!this.results.TryGetValue(project.Id, out cached!) || cached.Key != key)
            {
                cached = new Results(key);
                this.results[project.Id] = cached;
            }

            missing = analyzers.Where(a => !cached.ByAnalyzer.ContainsKey(a)).Distinct().ToList();
            compilerMissing = compilerKey is not null && !cached.Compiler.ContainsKey(compilerKey);
        }

        if (missing.Count > 0 || compilerMissing)
        {
            var compilation = await project.GetCompilationAsync(cancellation).ConfigureAwait(false);
            if (compilation is null)
            {
                return [];
            }

            var compiler = compilerMissing ? compilation.GetDiagnostics(cancellation).Where(d => compilerIds.Contains(d.Id)).ToImmutableArray() : [];
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>? found = null;
            if (missing.Count > 0)
            {
                running?.Invoke(missing.Count);
                var analysis = compilation.WithAnalyzers([.. missing], new CompilationWithAnalyzersOptions(project.AnalyzerOptions, null, concurrentAnalysis: true, logAnalyzerExecutionTime: false, reportSuppressedDiagnostics: false));
                var result = await analysis.GetAnalysisResultAsync(cancellation).ConfigureAwait(false);
                found = missing.ToImmutableDictionary(a => a, a => result.GetAllDiagnostics(a));
            }

            lock (this.results)
            {
                foreach (var pair in found ?? ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>.Empty)
                {
                    cached.ByAnalyzer[pair.Key] = pair.Value;
                }

                if (compilerMissing)
                {
                    cached.Compiler[compilerKey!] = compiler;
                }

                this.Runs += missing.Count + (compilerMissing ? 1 : 0);
            }
        }

        lock (this.results)
        {
            return Ordered(analyzers.Distinct().SelectMany(a => cached.ByAnalyzer[a]).Concat(compilerKey is null ? [] : cached.Compiler[compilerKey]));
        }
    }

    private sealed class Results((VersionStamp, VersionStamp, AnalyzerOptions) key)
    {
        public (VersionStamp Own, VersionStamp Dependent, AnalyzerOptions Options) Key { get; } = key;

        public Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> ByAnalyzer { get; } = [];

        public Dictionary<string, ImmutableArray<Diagnostic>> Compiler { get; } = [];
    }
}
