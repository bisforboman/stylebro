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

    /// <summary>Writes dotnet-format's --report file (ReportWriter.Write).</summary>
    public void WriteReport(string path, object files, ILogger logger) => this.writeReport.Invoke(null, [path, files, logger]);
}
