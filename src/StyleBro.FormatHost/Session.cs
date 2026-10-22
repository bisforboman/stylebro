using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace StyleBro.Host;

/// <summary>
/// One bro command's host: a workspace per configuration (MSBuildWorkspace, every framework of a multi-targeted project
/// loaded as its own project, the way a plain 'dotnet format' loads it), kept for every check of the command. Each check
/// runs on a view of it: the projects of its plan entry (for a framework's run, their instances of that framework) and the
/// projects they reference, the rest removed. Roslyn's solutions share what didn't change, so the projects the views have
/// in common compile once.
/// </summary>
internal sealed class Session(Func<string, IDisposable> measure) : IDisposable
{
    // Loading a workspace sets the run's environment for MSBuild's build host, which the process starts and which inherits it.
    private static readonly SemaphoreSlim EnvironmentGate = new(1, 1);

    // dotnet-format matches --include against the current folder: a check sets it for that step only.
    private static readonly SemaphoreSlim FolderGate = new(1, 1);

    private readonly ConcurrentDictionary<string, Lazy<Task<Loaded>>> workspaces = new(StringComparer.Ordinal);
    private readonly DotnetFormat format = DotnetFormat.Load();

    /// <summary>
    /// Runs one 'dotnet format' check (<paramref name="arguments"/> without 'format') and returns its exit code, or
    /// <see cref="Entry.Declined"/> for a command the host doesn't run.
    /// </summary>
    public int Run(string workspace, string folder, IReadOnlyList<string> environment, IReadOnlyList<string> selections, IReadOnlyList<string> arguments, Action<string> output)
    {
        if (FormatArguments.Parse(arguments) is not { Verify: true } parsed)
        {
            return Entry.Declined;
        }

        try
        {
            return this.RunAsync(workspace, folder, environment, selections, parsed, output).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            // What 'dotnet format' prints when it throws (bro's IsCrash reads it).
            output($"Unhandled exception: {exception}");
            return 1;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var loaded in this.workspaces.Values.Where(l => l.IsValueCreated && l.Value.IsCompletedSuccessfully))
        {
            loaded.Value.Result.Workspace.Dispose();
        }
    }

    private async Task<int> RunAsync(string workspace, string folder, IReadOnlyList<string> environment, IReadOnlyList<string> selections, FormatArguments arguments, Action<string> output)
    {
        var level = (LogLevel)FormatArguments.LogLevelOf(arguments.Verbosity);
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var plain = new HostLogger(level, output, names);
        plain.LogInformation(this.format.Text("Formatting_code_files_in_workspace_0"), arguments.Target);
        plain.LogTrace(this.format.Text("Loading_workspace"));
        var watch = Stopwatch.StartNew();
        var loaded = await this.LoadAsync(workspace, environment, arguments.IsSolution || !File.Exists(workspace) || !workspace.EndsWith("proj", StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
        if (loaded.Unsupported)
        {
            plain.LogError(this.format.Text("Could_not_format_0_Format_currently_supports_only_CSharp_and_Visual_Basic_projects"), workspace);
            return 1;
        }

        var view = Views.Of(loaded.Solution, selections, names);
        var logger = new HostLogger(level, output, names);

        // Like MSBuildWorkspaceLoader: one line about the load's warnings, or each of them at the diagnostic level.
        var warnings = Views.WarningsOf(loaded.Diagnostics, loaded.Solution, view);
        if (level == LogLevel.Trace)
        {
            foreach (var warning in warnings)
            {
                (warning.Kind == WorkspaceDiagnosticKind.Failure ? (Action<string>)(m => logger.LogError(m)) : m => logger.LogWarning(m))(warning.Message);
            }
        }
        else if (warnings.Count > 0)
        {
            logger.LogWarning(this.format.Text("Warnings_were_encountered_while_loading_the_workspace_Set_the_verbosity_option_to_the_diagnostic_level_to_log_warnings"));
        }

        var loadMs = watch.ElapsedMilliseconds;
        logger.LogTrace(this.format.Text("Complete_in_0_ms"), loadMs);
        logger.LogTrace(this.format.Text("Determining_formattable_files"));
        var options = this.format.Options(arguments, arguments.Target, level);
        var projectPath = arguments.IsSolution ? string.Empty : arguments.Target;
        (int Count, ImmutableArray<DocumentId> Documents) formattable;
        await FolderGate.WaitAsync().ConfigureAwait(false);
        var before = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = folder;
            formattable = await this.format.FormattableAsync(view, projectPath, options, logger, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            Environment.CurrentDirectory = before;
            FolderGate.Release();
        }

        var filesMs = watch.ElapsedMilliseconds - loadMs;
        logger.LogTrace(this.format.Text("Complete_in_0_ms"), filesMs);
        logger.LogTrace(this.format.Text("Running_formatters"));
        var (changed, files) = await this.format.FormatAsync(loaded.Workspace, view, formattable.Documents, options, logger, CancellationToken.None).ConfigureAwait(false);
        logger.LogTrace(this.format.Text("Complete_in_0_ms"), watch.ElapsedMilliseconds - loadMs - filesMs);
        foreach (var id in changed)
        {
            logger.LogInformation(this.format.Text("Formatted_code_file_0"), view.GetDocument(id)?.FilePath);
        }

        if (!string.IsNullOrWhiteSpace(arguments.Report))
        {
            this.format.WriteReport(Path.IsPathRooted(arguments.Report) ? arguments.Report! : Path.Combine(folder, arguments.Report!), files, logger);
        }

        logger.LogDebug(this.format.Text("Formatted_0_of_1_files"), changed.Count, formattable.Count);
        logger.LogInformation(this.format.Text("Format_complete_in_0_ms"), watch.ElapsedMilliseconds);
        return changed.Count == 0 ? 0 : 2;
    }

    private Task<Loaded> LoadAsync(string workspace, IReadOnlyList<string> environment, bool solution)
    {
        var key = workspace + "\n" + string.Join("\n", environment);
        return this.workspaces.GetOrAdd(key, _ => new Lazy<Task<Loaded>>(() => this.OpenAsync(workspace, environment, solution))).Value;
    }

    private async Task<Loaded> OpenAsync(string workspace, IReadOnlyList<string> environment, bool solution)
    {
        await EnvironmentGate.WaitAsync().ConfigureAwait(false);
        var previous = new List<(string Name, string? Value)>();
        try
        {
            using var phase = measure($"host: load workspace{(environment.FirstOrDefault(e => e.StartsWith("Configuration=", StringComparison.Ordinal)) is { } c ? $" ({c.Substring("Configuration=".Length)})" : string.Empty)}");
            foreach (var pair in environment)
            {
                var name = pair.Substring(0, pair.IndexOf('='));
                previous.Add((name, Environment.GetEnvironmentVariable(name)));
                Environment.SetEnvironmentVariable(name, pair.Substring(name.Length + 1));
            }

            // The same properties dotnet-format's loader sets (MSBuildWorkspaceLoader).
            var opened = MSBuildWorkspace.Create(new Dictionary<string, string>(StringComparer.Ordinal) { ["AlwaysCompileMarkupFilesInSeparateDomain"] = bool.FalseString });
            if (solution)
            {
                await opened.OpenSolutionAsync(workspace).ConfigureAwait(false);
            }
            else
            {
                try
                {
                    await opened.OpenProjectAsync(workspace).ConfigureAwait(false);
                }
                catch (InvalidOperationException)
                {
                    opened.Dispose();
                    return new Loaded(opened, opened.CurrentSolution, ImmutableList<WorkspaceDiagnostic>.Empty, Unsupported: true);
                }
            }

            return new Loaded(opened, opened.CurrentSolution, opened.Diagnostics, Unsupported: false);
        }
        finally
        {
            foreach (var (name, value) in previous)
            {
                Environment.SetEnvironmentVariable(name, value);
            }

            EnvironmentGate.Release();
        }
    }

    private sealed record Loaded(MSBuildWorkspace Workspace, Solution Solution, ImmutableList<WorkspaceDiagnostic> Diagnostics, bool Unsupported);
}
