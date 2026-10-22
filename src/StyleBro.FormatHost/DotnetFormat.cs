using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace StyleBro.Host;

/// <summary>
/// The pieces of the SDK's dotnet-format the host runs: its options, which files it formats, its formatters and its report.
/// They are internal to dotnet-format, so they're found by name; a missing one makes the host decline (bro then starts
/// 'dotnet format' as before) instead of checking another way. The finding lines then come from the same code as in
/// 'dotnet format' itself, word for word.
/// </summary>
internal sealed class DotnetFormat
{
    private const BindingFlags Statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly Type options;
    private readonly ConstructorInfo optionsConstructor;
    private readonly Type workspaceType;
    private readonly Type fixCategory;
    private readonly MethodInfo createMatcher;
    private readonly MethodInfo determineFiles;
    private readonly MethodInfo runFormatters;
    private readonly MethodInfo writeReport;
    private readonly Type formattedFile;
    private readonly PropertyInfo formattedDocument;
    private readonly Type resources;
    private readonly IEnumerable formatters;
    private readonly PropertyInfo category;
    private readonly MethodInfo formatterFormat;
    private readonly Type analyzerFormatter;
    private readonly MethodInfo filterAnalyzers;
    private readonly Type analysisResult;
    private readonly MethodInfo addDiagnostic;
    private readonly ConstructorInfo fileConstructor;
    private readonly ConstructorInfo changeConstructor;
    private readonly MethodInfo logDiagnosticIssue;

    private DotnetFormat(Assembly assembly)
    {
        Type Find(string name) => assembly.GetType(name, throwOnError: true)!;
        MethodInfo Method(Type type, string name) => type.GetMethod(name, Statics) ?? throw new MissingMethodException(type.FullName, name);
        this.options = Find("Microsoft.CodeAnalysis.Tools.FormatOptions");
        this.optionsConstructor = this.options.GetConstructors().Single(c => c.GetParameters().Length == 15);
        this.workspaceType = Find("Microsoft.CodeAnalysis.Tools.WorkspaceType");
        this.fixCategory = Find("Microsoft.CodeAnalysis.Tools.FixCategory");
        this.createMatcher = Method(Find("Microsoft.CodeAnalysis.Tools.Utilities.SourceFileMatcher"), "CreateMatcher");
        var formatter = Find("Microsoft.CodeAnalysis.Tools.CodeFormatter");
        this.determineFiles = Method(formatter, "DetermineFormattableFilesAsync");
        this.runFormatters = Method(formatter, "RunCodeFormattersAsync");
        this.writeReport = Method(Find("Microsoft.CodeAnalysis.Tools.ReportWriter"), "Write");
        this.formattedFile = Find("Microsoft.CodeAnalysis.Tools.FormattedFile");
        this.formattedDocument = this.formattedFile.GetProperty("DocumentId") ?? throw new MissingMemberException(this.formattedFile.FullName, "DocumentId");
        this.resources = Find("Microsoft.CodeAnalysis.Tools.Resources");

        // The fixes (phase 2): dotnet-format's formatters in its order, the analyzer formatters' parts.
        this.formatters = (IEnumerable)(formatter.GetField("s_codeFormatters", Statics) ?? throw new MissingFieldException(formatter.FullName, "s_codeFormatters")).GetValue(null)!;
        var codeFormatter = Find("Microsoft.CodeAnalysis.Tools.Formatters.ICodeFormatter");
        this.category = codeFormatter.GetProperty("Category") ?? throw new MissingMemberException(codeFormatter.FullName, "Category");
        this.formatterFormat = codeFormatter.GetMethod("FormatAsync") ?? throw new MissingMethodException(codeFormatter.FullName, "FormatAsync");
        this.analyzerFormatter = Find("Microsoft.CodeAnalysis.Tools.Analyzers.AnalyzerFormatter");
        this.filterAnalyzers = Method(this.analyzerFormatter, "FilterAnalyzersAsync");
        this.analysisResult = Find("Microsoft.CodeAnalysis.Tools.Analyzers.CodeAnalysisResult");
        this.addDiagnostic = this.analysisResult.GetMethod("AddDiagnostic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingMethodException(this.analysisResult.FullName, "AddDiagnostic");
        this.fileConstructor = this.formattedFile.GetConstructors().Single(c => c.GetParameters().Length == 2);
        this.changeConstructor = Find("Microsoft.CodeAnalysis.Tools.FileChange").GetConstructors().Single(c => c.GetParameters().Length == 3);
        this.logDiagnosticIssue = Method(Find("Microsoft.CodeAnalysis.Tools.ILoggerExtensions"), "LogDiagnosticIssue");
        foreach (var formatterObject in this.formatters)
        {
            if (this.analyzerFormatter.IsInstanceOfType(formatterObject))
            {
                _ = this.Parts(formatterObject);
            }
        }
        _ = this.Text("Warnings_were_encountered_while_loading_the_workspace_Set_the_verbosity_option_to_the_diagnostic_level_to_log_warnings");
    }

    /// <summary>The bridge to the dotnet-format next to the host's Roslyn (its load context resolves it from the SDK).</summary>
    public static DotnetFormat Load() =>
        new(AssemblyLoadContext.GetLoadContext(typeof(DotnetFormat).Assembly)!.LoadFromAssemblyName(new AssemblyName("dotnet-format")));

    /// <summary>One of dotnet-format's messages ("Complete in {0}ms.").</summary>
    public string Text(string name) =>
        this.resources.GetProperty(name, Statics)?.GetValue(null) as string ?? throw new MissingMemberException(this.resources.FullName, name);

    /// <summary>dotnet-format's options for <paramref name="arguments"/>, as its command line would make them.</summary>
    public object Options(FormatArguments arguments, string target, LogLevel level)
    {
        var severity = (DiagnosticSeverity)FormatArguments.SeverityOf(arguments.Severity);
        var category = FormatArguments.Categories(arguments.Pass);
        var values = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["WorkspaceFilePath"] = target,
            ["WorkspaceType"] = Enum.ToObject(this.workspaceType, arguments.IsSolution ? 2 : 1),
            ["NoRestore"] = arguments.NoRestore,
            ["LogLevel"] = level,
            ["FixCategory"] = Enum.ToObject(this.fixCategory, category),

            // Like its commands: the plain one takes --severity for both, style and analyzers for their own.
            ["CodeStyleSeverity"] = category is 7 or 2 ? severity : DiagnosticSeverity.Warning,
            ["AnalyzerSeverity"] = category is 7 or 4 ? severity : DiagnosticSeverity.Warning,
            ["Diagnostics"] = category == 1 || arguments.Diagnostics.Count == 0 ? ImmutableHashSet<string>.Empty : arguments.Diagnostics.ToImmutableHashSet(),
            ["ExcludeDiagnostics"] = category == 1 || arguments.ExcludeDiagnostics.Count == 0 ? ImmutableHashSet<string>.Empty : arguments.ExcludeDiagnostics.ToImmutableHashSet(),
            ["SaveFormattedFiles"] = !arguments.Verify,
            ["ChangesAreErrors"] = arguments.Verify,
            ["FileMatcher"] = this.createMatcher.Invoke(null, [arguments.Include.ToArray(), arguments.Exclude.ToArray()]),
            ["ReportPath"] = arguments.Report,
            ["BinaryLogPath"] = null,
            ["IncludeGeneratedFiles"] = arguments.IncludeGenerated,
        };
        return this.optionsConstructor.Invoke(this.optionsConstructor.GetParameters().Select(p => values.TryGetValue(p.Name!, out var value) ? value : throw new MissingMemberException(this.options.FullName, p.Name)).ToArray());
    }

    /// <summary>The documents dotnet-format formats in <paramref name="solution"/> (CodeFormatter.DetermineFormattableFilesAsync).</summary>
    public async Task<(int Count, ImmutableArray<DocumentId> Documents)> FormattableAsync(Solution solution, string projectPath, object options, ILogger logger, CancellationToken cancellation)
    {
        var task = (Task)this.determineFiles.Invoke(null, [solution, projectPath, options, logger, cancellation])!;
        await task.ConfigureAwait(false);
        var result = ((int, ImmutableArray<DocumentId>))task.GetType().GetProperty("Result")!.GetValue(task)!;
        return (result.Item1, result.Item2);
    }

    /// <summary>
    /// Runs dotnet-format's formatters on <paramref name="documents"/> (CodeFormatter.RunCodeFormattersAsync): a check logs
    /// what it would change. Returns the changed documents, in the order it found them, and the list its report writes.
    /// </summary>
    public async Task<(List<DocumentId> Changed, object Files)> FormatAsync(Workspace workspace, Solution solution, ImmutableArray<DocumentId> documents, object options, ILogger logger, CancellationToken cancellation)
    {
        var files = Activator.CreateInstance(typeof(List<>).MakeGenericType(this.formattedFile))!;
        var task = (Task)this.runFormatters.Invoke(null, [workspace, solution, documents, options, logger, files, cancellation])!;
        await task.ConfigureAwait(false);
        var changed = ((IEnumerable)files).Cast<object>().Select(f => (DocumentId)this.formattedDocument.GetValue(f)!).Distinct().ToList();
        return (changed, files);
    }

    /// <summary>
    /// dotnet-format's formatters in its order (CodeFormatter.s_codeFormatters): each one's fix categories (1 whitespace, 2 code
    /// style, 4 analyzers) and, for its two analyzer formatters (code style, then the projects' analyzer references), their parts.
    /// </summary>
    public List<(object Formatter, int Category, AnalyzerParts? Analyzers)> Formatters() =>
        this.formatters.Cast<object>().Select(f => (f, Convert.ToInt32(this.category.GetValue(f)), this.analyzerFormatter.IsInstanceOfType(f) ? this.Parts(f) : null)).ToList();

    /// <summary>Runs one of dotnet-format's formatters other than the analyzer ones (ICodeFormatter.FormatAsync).</summary>
    public async Task<Solution> RunFormatterAsync(object formatter, Workspace workspace, Solution solution, ImmutableArray<DocumentId> documents, object options, ILogger logger, object files, CancellationToken cancellation)
    {
        var task = (Task<Solution>)this.formatterFormat.Invoke(formatter, [workspace, solution, documents, options, logger, files, cancellation])!;
        return await task.ConfigureAwait(false);
    }

    /// <summary>A FormatOptions value by its name (Diagnostics, LogLevel, SaveFormattedFiles, ...).</summary>
    public static T Option<T>(object options, string name) => (T)options.GetType().GetProperty(name)!.GetValue(options)!;

    /// <summary>
    /// The analyzers and fixers of each project, as the formatter's IAnalyzerInformationProvider finds them, and the raw value
    /// <see cref="FilterAnalyzersAsync"/> takes.
    /// </summary>
    public (Dictionary<ProjectId, (ImmutableArray<DiagnosticAnalyzer> Analyzers, ImmutableArray<CodeFixProvider> Fixers)> Projects, object Raw) AnalyzersAndFixers(AnalyzerParts parts, Workspace workspace, Solution solution, object options, ILogger logger)
    {
        var raw = parts.Provider.GetType().GetMethod("GetAnalyzersAndFixers")!.Invoke(parts.Provider, [workspace, solution, options, logger])!;
        var projects = new Dictionary<ProjectId, (ImmutableArray<DiagnosticAnalyzer>, ImmutableArray<CodeFixProvider>)>();
        foreach (var pair in (IEnumerable)raw)
        {
            var key = (ProjectId)pair.GetType().GetProperty("Key")!.GetValue(pair)!;
            var value = pair.GetType().GetProperty("Value")!.GetValue(pair)!;
            projects[key] = ((ImmutableArray<DiagnosticAnalyzer>)value.GetType().GetProperty("Analyzers")!.GetValue(value)!, (ImmutableArray<CodeFixProvider>)value.GetType().GetProperty("Fixers")!.GetValue(value)!);
        }

        return (projects, raw);
    }

    /// <summary>The minimum severity the formatter fixes (IAnalyzerInformationProvider.GetSeverity).</summary>
    public static DiagnosticSeverity Severity(AnalyzerParts parts, object options) =>
        (DiagnosticSeverity)parts.Provider.GetType().GetMethod("GetSeverity")!.Invoke(parts.Provider, [options])!;

    /// <summary>The analyzers each project runs (AnalyzerFormatter.FilterAnalyzersAsync: language, ids, severity).</summary>
    public async Task<ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticAnalyzer>>> FilterAnalyzersAsync(Solution solution, object raw, ImmutableHashSet<string> paths, DiagnosticSeverity severity, object options, CancellationToken cancellation)
    {
        var task = (Task)this.filterAnalyzers.Invoke(null, [solution, raw, paths, severity, Option<ImmutableHashSet<string>>(options, "Diagnostics"), Option<ImmutableHashSet<string>>(options, "ExcludeDiagnostics"), cancellation])!;
        await task.ConfigureAwait(false);
        return (ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticAnalyzer>>)task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    /// <summary>A CodeAnalysisResult with <paramref name="diagnostics"/> (AddDiagnostic drops the ids the options leave out).</summary>
    public object AnalysisResult(object options, IEnumerable<(Project Project, Diagnostic Diagnostic)> diagnostics)
    {
        var result = Activator.CreateInstance(this.analysisResult, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [Option<ImmutableHashSet<string>>(options, "Diagnostics"), Option<ImmutableHashSet<string>>(options, "ExcludeDiagnostics")], null)!;
        foreach (var (project, diagnostic) in diagnostics)
        {
            this.addDiagnostic.Invoke(result, [project, diagnostic]);
        }

        return result;
    }

    /// <summary>Whether CodeAnalysisResult.AddDiagnostic keeps <paramref name="id"/> (--diagnostics, --exclude-diagnostics).</summary>
    public static bool Accepts(object options, string id)
    {
        var diagnostics = Option<ImmutableHashSet<string>>(options, "Diagnostics");
        var excluded = Option<ImmutableHashSet<string>>(options, "ExcludeDiagnostics");
        return (excluded.IsEmpty || !excluded.Contains(id)) && (diagnostics.IsEmpty || diagnostics.Contains(id));
    }

    /// <summary>Whether a CodeAnalysisResult has a diagnostic.</summary>
    public static bool HasDiagnostics(object result) =>
        ((IEnumerable)result.GetType().GetProperty("Diagnostics")!.GetValue(result)!).Cast<object>().Any(p => ((ICollection)p.GetType().GetProperty("Value")!.GetValue(p)!).Count > 0);

    /// <summary>One Fix All of <paramref name="fixer"/> for <paramref name="id"/> (SolutionCodeFixApplier.ApplyCodeFixesAsync).</summary>
    public static async Task<Solution> ApplyAsync(AnalyzerParts parts, Solution solution, object result, CodeFixProvider fixer, string id, ILogger logger, CancellationToken cancellation)
    {
        var task = (Task<Solution>)parts.Applier.GetType().GetMethod("ApplyCodeFixesAsync")!.Invoke(parts.Applier, [solution, result, fixer, id, logger, cancellation])!;
        return await task.ConfigureAwait(false);
    }

    /// <summary>Adds a diagnostic to the report's list (a FormattedFile with one FileChange), as AnalyzerFormatter does.</summary>
    public void AddFile(object files, Document document, LinePosition position, string id, string description)
    {
        var changes = Array.CreateInstance(this.changeConstructor.DeclaringType!, 1);
        changes.SetValue(this.changeConstructor.Invoke([position, id, description]), 0);
        ((IList)files).Add(this.fileConstructor.Invoke([document, changes]));
    }

    /// <summary>Logs a diagnostic the way dotnet-format does (ILoggerExtensions.LogDiagnosticIssue).</summary>
    public void LogDiagnostic(ILogger logger, Document document, LinePosition position, Diagnostic diagnostic, bool changesAreErrors) =>
        this.logDiagnosticIssue.Invoke(null, [logger, document, position, diagnostic, changesAreErrors]);

    /// <summary>A new list for <see cref="FormatAsync"/>'s and the formatters' FormattedFile entries.</summary>
    public object NewFiles() => Activator.CreateInstance(typeof(List<>).MakeGenericType(this.formattedFile))!;

    /// <summary>The documents a list of FormattedFile entries names, in order, once each.</summary>
    public List<DocumentId> Documents(object files) => ((IEnumerable)files).Cast<object>().Select(f => (DocumentId)this.formattedDocument.GetValue(f)!).Distinct().ToList();

    /// <summary>Writes dotnet-format's --report file (ReportWriter.Write).</summary>
    public void WriteReport(string path, object files, ILogger logger) => this.writeReport.Invoke(null, [path, files, logger]);

    private AnalyzerParts Parts(object formatter)
    {
        object Field(string name) => this.analyzerFormatter.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(formatter) ?? throw new MissingFieldException(this.analyzerFormatter.FullName, name);
        return new AnalyzerParts((string)Field("_name"), (bool)Field("_includeCompilerDiagnostics"), Field("_informationProvider"), Field("_applier"));
    }

    /// <summary>
    /// An AnalyzerFormatter's parts: its name ("Code Style", "Analyzer Reference"), whether it fixes compiler diagnostics, where
    /// it finds the analyzers and fixers, and what applies a Fix All.
    /// </summary>
    public sealed record AnalyzerParts(string Name, bool IncludeCompilerDiagnostics, object Provider, object Applier);
}
