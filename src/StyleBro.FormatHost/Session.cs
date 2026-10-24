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

    // Fix runs one at a time: each must see the one before it, and they set the fixers' variables in this process.
    private static readonly SemaphoreSlim FixGate = new(1, 1);

    private readonly ConcurrentDictionary<string, Lazy<Task<Loaded>>> workspaces = new(StringComparer.Ordinal);

    // The workspaces as a copy of the repository has the files (Run with a copy): per copy and workspace.
    private readonly ConcurrentDictionary<string, Lazy<Task<Loaded>>> copies = new(StringComparer.Ordinal);
    private readonly DotnetFormat format = DotnetFormat.Load();
    private readonly AnalysisCache cache = new();
    private readonly Func<string, IDisposable> measure = measure;

    /// <summary>
    /// Runs one 'dotnet format' command (<paramref name="arguments"/> without 'format'), a check or a fix run, and returns its exit
    /// code, or <see cref="Entry.Declined"/> for a command the host doesn't run. <paramref name="variables"/> ('Name=value') are
    /// set in this process while a fix run applies its fixes: what bro gives a 'dotnet format' process that the fixers read
    /// (the repository's names), not what changes how the projects load.
    /// </summary>
    public int Run(string workspace, string folder, IReadOnlyList<string> environment, IReadOnlyList<string> variables, IReadOnlyList<string> selections, IReadOnlyList<string> arguments, Action<string> output, CopyOf? copy = null)
    {
        if (FormatArguments.Parse(arguments) is not { } parsed)
        {
            return Entry.Declined;
        }

        try
        {
            return this.RunAsync(workspace, folder, environment, variables, selections, parsed, output, copy).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            // What 'dotnet format' prints when it throws (bro's IsCrash reads it).
            output($"Unhandled exception: {exception}");
            return 1;
        }
    }

    /// <summary>Drops what the runs on <paramref name="copy"/> kept (<see cref="CopyOf"/>): the copy is gone.</summary>
    public void Forget(string copy)
    {
        foreach (var key in this.copies.Keys.Where(k => k.EndsWith("\n" + copy, StringComparison.Ordinal)).ToList())
        {
            this.copies.TryRemove(key, out _);
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

    private async Task<int> RunAsync(string workspace, string folder, IReadOnlyList<string> environment, IReadOnlyList<string> variables, IReadOnlyList<string> selections, FormatArguments arguments, Action<string> output, CopyOf? copy)
    {
        var level = (LogLevel)FormatArguments.LogLevelOf(arguments.Verbosity);
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var plain = new HostLogger(level, output, names);
        plain.LogInformation(this.format.Text("Formatting_code_files_in_workspace_0"), arguments.Target);
        plain.LogTrace(this.format.Text("Loading_workspace"));
        var watch = Stopwatch.StartNew();
        var loaded = await this.LoadAsync(workspace, environment, arguments.IsSolution || !File.Exists(workspace) || !workspace.EndsWith("proj", StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
        if (loaded.Failure is { } failure)
        {
            output(Entry.LoadFailedMarker + failure);
            return Entry.LoadFailed;
        }

        if (loaded.Unsupported)
        {
            plain.LogError(this.format.Text("Could_not_format_0_Format_currently_supports_only_CSharp_and_Visual_Basic_projects"), workspace);
            return 1;
        }

        if (copy is not null)
        {
            loaded = await this.CopyAsync(workspace, environment, loaded, copy).ConfigureAwait(false);

            // A file the copy doesn't have (git ignores it) that the build didn't write: a load of the copy wouldn't have it.
            if (loaded.Missing)
            {
                return Entry.Declined;
            }
        }

        // The files as they are now: what bro and the runs before this one changed on disk.
        var current = await loaded.SyncAsync(this.measure).ConfigureAwait(false);
        var view = Views.Of(current, selections, names);
        var logger = new HostLogger(level, output, names);

        // Like MSBuildWorkspaceLoader: one line about the load's warnings, or each of them at the diagnostic level.
        var warnings = Views.WarningsOf(loaded.Diagnostics, loaded.Solution, view, Views.FrameworksOf(selections));
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
        var files = this.format.NewFiles();
        var loop = new FixLoop(this.format, this.cache, this.measure, FixInteractions.Default);
        if (arguments.Verify)
        {
            await loop.RunAsync(loaded.Workspace, view, formattable.Documents, options, logger, files, CancellationToken.None).ConfigureAwait(false);
        }
        else
        {
            await FixGate.WaitAsync().ConfigureAwait(false);
            var previous = Set(variables);
            try
            {
                var formatted = await loop.RunAsync(loaded.Workspace, view, formattable.Documents, options, logger, files, CancellationToken.None).ConfigureAwait(false);
                await loaded.SaveAsync(view, formatted).ConfigureAwait(false);
            }
            finally
            {
                Set(previous);
                FixGate.Release();
            }
        }

        var changed = this.format.Documents(files);
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
        return !arguments.Verify || changed.Count == 0 ? 0 : 2;
    }

    // Sets 'Name=value' variables in this process; returns what they were.
    private static List<string> Set(IReadOnlyList<string> variables)
    {
        var previous = new List<string>();
        foreach (var pair in variables)
        {
            var name = pair.Substring(0, pair.IndexOf('='));
            previous.Add($"{name}={Environment.GetEnvironmentVariable(name)}");
            var value = pair.Substring(name.Length + 1);
            Environment.SetEnvironmentVariable(name, value.Length == 0 ? null : value);
        }

        return previous;
    }

    private Task<Loaded> LoadAsync(string workspace, IReadOnlyList<string> environment, bool solution)
    {
        var key = workspace + "\n" + string.Join("\n", environment);
        return this.workspaces.GetOrAdd(key, _ => new Lazy<Task<Loaded>>(() => this.OpenAsync(workspace, environment, solution))).Value;
    }

    // The loaded workspace as the copy has its files: it starts from the solution as the repository's files are now, and reads
    // every file back from the copy at its first run, so only what differs there changes (and compiles again).
    private Task<Loaded> CopyAsync(string workspace, IReadOnlyList<string> environment, Loaded loaded, CopyOf copy)
    {
        var key = workspace + "\n" + string.Join("\n", environment) + "\n" + copy.Copy;
        return this.copies.GetOrAdd(key, _ => new Lazy<Task<Loaded>>(async () =>
        {
            var repository = await loaded.SyncAsync(this.measure).ConfigureAwait(false);
            return new Loaded(loaded.Workspace, loaded.Solution, loaded.Diagnostics, unsupported: false, disk: new DiskState(copy.OnDisk), current: repository) { Missing = copy.Lacks(repository) };
        })).Value;
    }

    private async Task<Loaded> OpenAsync(string workspace, IReadOnlyList<string> environment, bool solution)
    {
        await EnvironmentGate.WaitAsync().ConfigureAwait(false);
        var previous = new List<(string Name, string? Value)>();
        try
        {
            using var phase = this.measure($"host: load workspace{(environment.FirstOrDefault(e => e.StartsWith("Configuration=", StringComparison.Ordinal)) is { } c ? $" ({c.Substring("Configuration=".Length)})" : string.Empty)}");
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
                try
                {
                    await opened.OpenSolutionAsync(workspace).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Trial 50, ncalc: a file one project has as an additional file and another compiles ("The solution does
                    // not contain the specified document", SDK 10.0.1xx). bro runs 'dotnet format' instead, which it gives a
                    // project file then; a solution 'dotnet format' can't open either fails there as it would without the host.
                    opened.Dispose();
                    return new Loaded(opened, opened.CurrentSolution, ImmutableList<WorkspaceDiagnostic>.Empty, unsupported: false, failure: exception.Message);
                }
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
                    return new Loaded(opened, opened.CurrentSolution, ImmutableList<WorkspaceDiagnostic>.Empty, unsupported: true);
                }
            }

            var loaded = new Loaded(opened, opened.CurrentSolution, opened.Diagnostics, unsupported: false);
            loaded.Disk.Remember(opened.CurrentSolution);
            return loaded;
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

    /// <summary>A configuration's workspace: the solution as loaded, and as the files are now (<see cref="DiskState"/>).</summary>
    private sealed class Loaded
    {
        private readonly SemaphoreSlim gate = new(1, 1);
        private Solution current;

        public Loaded(MSBuildWorkspace workspace, Solution solution, ImmutableList<WorkspaceDiagnostic> diagnostics, bool unsupported, string? failure = null, DiskState? disk = null, Solution? current = null)
        {
            this.Workspace = workspace;
            this.Solution = solution;
            this.current = current ?? solution;
            this.Disk = disk ?? new DiskState();
            this.Diagnostics = diagnostics;
            this.Unsupported = unsupported;
            this.Failure = failure;
        }

        public MSBuildWorkspace Workspace { get; }

        public Solution Solution { get; }

        public ImmutableList<WorkspaceDiagnostic> Diagnostics { get; }

        public bool Unsupported { get; }

        /// <summary>Why the workspace didn't load, or null (<see cref="Entry.LoadFailed"/>).</summary>
        public string? Failure { get; }

        public DiskState Disk { get; }

        /// <summary>Gets a value indicating whether a copy lacks one of the documents' files (<see cref="CopyOf.Lacks"/>).</summary>
        public bool Missing { get; init; }

        /// <summary>The solution with the files as they are on disk now.</summary>
        public async Task<Solution> SyncAsync(Func<string, IDisposable> measure)
        {
            await this.gate.WaitAsync().ConfigureAwait(false);
            try
            {
                using var phase = measure("host: read changed files");
                (this.current, _) = await this.Disk.SyncAsync(this.current, CancellationToken.None).ConfigureAwait(false);
                return this.current;
            }
            finally
            {
                this.gate.Release();
            }
        }

        /// <summary>Writes what a fix run changed in its view and keeps it in every copy of those files.</summary>
        public async Task SaveAsync(Solution view, Solution formatted)
        {
            await this.gate.WaitAsync().ConfigureAwait(false);
            try
            {
                (this.current, _) = await this.Disk.SaveAsync(this.current, view, formatted, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                this.gate.Release();
            }
        }
    }
}
