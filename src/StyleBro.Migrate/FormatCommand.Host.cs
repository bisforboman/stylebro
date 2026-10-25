using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StyleBro.Migrate;

/// <summary>
/// docs/proposals/format-speed.md (owner's decisions 2026-10-17): the checks and fixes run in an in-process host
/// (<see cref="FormatHost"/>), the default since 2026-10-25; --engine dotnet-format runs them in 'dotnet format' processes,
/// as before.
/// </summary>
internal static partial class FormatCommand
{
    /// <summary>Picks what runs the checks and fixes: <see cref="HostEngine"/> (the default) or <see cref="DotnetFormatEngine"/>.</summary>
    public const string EngineOption = "--engine";

    /// <summary>The in-process host (the default since 2026-10-25, owner's decision).</summary>
    public const string HostEngine = "host";

    /// <summary>A 'dotnet format' process per run and pass: the default until 2026-10-25, kept as a fallback.</summary>
    public const string DotnetFormatEngine = "dotnet-format";

    private static readonly System.Threading.AsyncLocal<string?> Engine = new();

    // The command's hosts, one per folder it formats (the repository, a preview's copy): each keeps its workspaces for every
    // run of the command.
    private static readonly System.Threading.AsyncLocal<Dictionary<string, HostScope>?> Hosts = new();

    // The copies check is formatting (CopyHost): their runs use the repository's host.
    private static readonly System.Threading.AsyncLocal<List<CopyMap>?> Copies = new();

    /// <summary>Gets the engine this command uses (<see cref="UseEngine"/>).</summary>
    public static string CurrentEngine => Engine.Value ?? HostEngine;

    /// <summary>
    /// Takes --engine out of <paramref name="args"/>: the engine it names, else <see cref="HostEngine"/>; null and
    /// <paramref name="error"/> for a value that is neither.
    /// </summary>
    public static string? TakeEngine(ref string[] args, out string? error)
    {
        error = null;
        var at = Array.IndexOf(args, EngineOption);
        if (at < 0)
        {
            return HostEngine;
        }

        var value = at + 1 < args.Length ? args[at + 1] : null;
        if (value is not (HostEngine or DotnetFormatEngine))
        {
            error = $"{EngineOption} takes '{HostEngine}' (the default: checks and fixes run in this process) or '{DotnetFormatEngine}' (a 'dotnet format' process per run, as before 2026-10-25).";
            return null;
        }

        args = args.Take(at).Concat(args.Skip(at + 2)).ToArray();
        return value;
    }

    /// <summary>Uses <paramref name="engine"/> for this command (an async-local, like <see cref="PhaseTimes"/>) until disposed.</summary>
    public static IDisposable UseEngine(string engine)
    {
        var before = Engine.Value;
        var hostsBefore = Hosts.Value;
        var hosts = new Dictionary<string, HostScope>(StringComparer.Ordinal);
        Engine.Value = engine;
        Hosts.Value = hosts;
        return new EngineScope(() =>
        {
            foreach (var host in hosts.Values)
            {
                host.Close();
            }

            Engine.Value = before;
            Hosts.Value = hostsBefore;
        });
    }

    /// <summary>
    /// With the host engine, makes the runs on <paramref name="copy"/>, a copy of <paramref name="source"/>, use the host of the
    /// repository's runs until disposed: its workspaces, as loaded and analyzed for the repository, with the copy's files (the
    /// paths mapped both ways), instead of loading the copy's (check's kept findings: PalCalc loaded its three configurations
    /// again). A copy run whose folder has no host of the repository's opens its own, as before.
    /// </summary>
    public static IDisposable CopyHost(string copy, string source)
    {
        if (CurrentEngine != HostEngine)
        {
            return new EngineScope(() => { });
        }

        var map = new CopyMap(WithSeparator(copy), WithSeparator(source));
        var before = Copies.Value;
        Copies.Value = [.. before ?? [], map];
        return new EngineScope(() =>
        {
            Copies.Value = before;
            map.Forget();
            if (Hosts.Value is { } hosts)
            {
                lock (hosts)
                {
                    foreach (var key in hosts.Where(h => h.Value.Copy == map).Select(h => h.Key).ToList())
                    {
                        hosts[key].Close();
                        hosts.Remove(key);
                    }
                }
            }
        });
    }

    /// <summary><paramref name="text"/> with every path in <paramref name="from"/> (a folder ending in a separator) moved to <paramref name="to"/>.</summary>
    internal static string MovePaths(string text, string from, string to) =>
        text == from.TrimEnd(Path.DirectorySeparatorChar) ? to.TrimEnd(Path.DirectorySeparatorChar) : text.Replace(from, to, StringComparison.Ordinal);

    private static string WithSeparator(string folder) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)) + Path.DirectorySeparatorChar;

    /// <summary>
    /// The environment a run's 'dotnet format' process gets that the host's workspace load needs too: the configuration and
    /// bro's hook, not the framework (the host loads every framework and picks the run's projects from them).
    /// </summary>
    private static List<string> HostEnvironment(RunEnvironment? run)
    {
        if (run is null)
        {
            return [];
        }

        var environment = new Dictionary<string, string?>(StringComparer.Ordinal) { ["BeforeMicrosoftNETSdkTargets"] = run.SelectTargets };
        if (run.Warnings)
        {
            environment["StyleBroFormatWarnings"] = "true";
        }

        if (run.OldCSharp is not null)
        {
            environment["StyleBroFormatOldCSharp"] = run.OldCSharp;
        }

        if (run.Configuration.Length > 0)
        {
            SetConfiguration(environment, run.Configuration);
        }

        return environment.OrderBy(e => e.Key, StringComparer.Ordinal).Select(e => $"{e.Key}={e.Value}").ToList();
    }

    /// <summary>
    /// What else a run's 'dotnet format' process gets that its fixers read: the file of the repository's names (a run that
    /// loads only some projects). The host sets it while it applies that run's fixes.
    /// </summary>
    private static List<string> HostVariables(RunEnvironment? run) => run?.Names is { } names ? [$"{StyleBro.Analyzers.RepositoryNames.Variable}={names}"] : [];

    /// <summary>
    /// The host of a 'format' or 'check' command with the host engine for one folder, opened at its first run and closed with the
    /// command (every run of the command uses it: its workspaces stay loaded); null with --engine dotnet-format. A host that can't
    /// run here says why once and runs nothing.
    /// </summary>
    private sealed class HostScope(string root, Action<string> log, CopyMap? copy = null, HostScope? source = null) : IDisposable
    {
        private readonly object gate = new();
        private bool opened;
        private FormatHost? host;
        private FormatHost? retired;
        private HostScope? own;

        /// <summary>Gets the copy this scope's runs are on (<see cref="CopyHost"/>), or null.</summary>
        public CopyMap? Copy => copy;

        /// <summary>Gets the folder this scope's runs format.</summary>
        public string Root => root;

        public static HostScope? For(string root, Action<string> log)
        {
            if (CurrentEngine != HostEngine)
            {
                return null;
            }

            var hosts = Hosts.Value ??= new Dictionary<string, HostScope>(StringComparer.Ordinal);
            lock (hosts)
            {
                if (!hosts.TryGetValue(root, out var scope))
                {
                    // A copy check formats: the repository's host, if its runs opened one.
                    var copy = Copies.Value?.LastOrDefault(c => WithSeparator(root).StartsWith(c.Copy, StringComparison.Ordinal));
                    var source = copy is not null && hosts.TryGetValue(MovePaths(root, copy.Copy, copy.Source), out var found) && found.Copy is null ? found : null;
                    hosts[root] = scope = source is not null ? new HostScope(root, log, copy, source) : new HostScope(root, log);
                }

                return scope;
            }
        }

        /// <summary>
        /// Runs a check in the host (<see cref="FormatHost.Run"/>), or 'dotnet format' (<paramref name="otherwise"/>) when the
        /// host can't run here or doesn't run that command.
        /// </summary>
        public int Run(string workspace, RunEnvironment? run, IEnumerable<string> selections, IEnumerable<string> arguments, Action<string> output, Func<int> otherwise)
        {
            if (copy is not null && source is not null)
            {
                // A copy the repository's workspace doesn't fit (a file it lacks): a host of its own, as before, for all its runs.
                HostScope Own()
                {
                    lock (this.gate)
                    {
                        return this.own ??= new HostScope(root, log);
                    }
                }

                lock (this.gate)
                {
                    if (this.own is not null)
                    {
                        return this.own.Run(workspace, run, selections, arguments, output, otherwise);
                    }
                }

                return RunOnCopy(this, copy, source, workspace, run, selections, arguments, output, () => Own().Run(workspace, run, selections, arguments, output, otherwise));
            }

            FormatHost? open;
            lock (this.gate)
            {
                if (!this.opened)
                {
                    this.opened = true;
                    this.host = FormatHost.Open(root, log);
                }

                open = this.host;
            }

            string? failure = null;
            if (open is not null && open.Run(workspace, root, HostEnvironment(run), HostVariables(run), selections, arguments, output, out failure) is { } code)
            {
                return code;
            }

            // The workspace didn't load (trial 50, ncalc): this run and the rest of the command run 'dotnet format'.
            if (failure is not null)
            {
                lock (this.gate)
                {
                    if (ReferenceEquals(this.host, open))
                    {
                        // Disposed with the command (Close): checks running at once may still be using it.
                        log(FormatHost.LoadFailedLine(failure));
                        this.retired = open;
                        this.host = null;
                    }
                }
            }

            return otherwise();
        }

        /// <summary>The repository's open host, or null (not opened, or retired after a failed load).</summary>
        public FormatHost? Open()
        {
            lock (this.gate)
            {
                return this.host;
            }
        }

        /// <summary>Nothing: the command closes its hosts (<see cref="Close"/>), so the next run finds its workspaces loaded.</summary>
        public void Dispose()
        {
        }

        public void Close()
        {
            lock (this.gate)
            {
                this.host?.Dispose();
                this.retired?.Dispose();
                this.own?.Close();
                this.host = null;
                this.retired = null;
            }
        }
    }

    /// <summary>
    /// A run on a copy in the repository's host (<see cref="CopyHost"/>): what it is given in the copy's paths, what it prints
    /// back in them. The files the run hands the host that name paths (the old C# list, the repository's names) are written
    /// again with the repository's: the old C# list under the name the repository's runs gave it, which the workspace is keyed by.
    /// </summary>
    private static int RunOnCopy(HostScope scope, CopyMap copy, HostScope source, string workspace, RunEnvironment? run, IEnumerable<string> selections, IEnumerable<string> arguments, Action<string> output, Func<int> otherwise)
    {
        if (source.Open() is not { } open)
        {
            return otherwise();
        }

        string In(string text) => MovePaths(text, copy.Copy, copy.Source);
        var written = new List<string>();
        try
        {
            var environment = HostEnvironment(run).Select(e => e.StartsWith("StyleBroFormatOldCSharp=", StringComparison.Ordinal) ? "StyleBroFormatOldCSharp=" + OldCSharpIn(e["StyleBroFormatOldCSharp=".Length..], In, written) : In(e)).ToList();
            var variables = HostVariables(run).Select(v => v[..(v.IndexOf('=') + 1)] + FileIn(v[(v.IndexOf('=') + 1)..], In, written)).ToList();
            copy.Used(open);
            if (open.Run((copy.Copy, copy.Source), In(workspace), In(scope.Root), environment, variables, selections.Select(In), arguments.Select(In), line => output(MovePaths(line, copy.Source, copy.Copy)), out _) is { } code)
            {
                return code;
            }
        }
        finally
        {
            written.ForEach(File.Delete);
        }

        return otherwise();
    }

    // The old C# list with the repository's paths, at the path the repository's runs wrote it to (same lines, same name).
    private static string OldCSharpIn(string path, Func<string, string> map, List<string> written)
    {
        if (!File.Exists(path))
        {
            return map(path);
        }

        var lines = File.ReadAllLines(path).Select(map).ToList();
        var moved = RunFile("stylebro-", StyleBro.Analyzers.Modernize.MultiTargetSuppressor.OldCSharpExtension, string.Join("\n", lines));
        if (!File.Exists(moved))
        {
            File.WriteAllLines(moved, lines);
            written.Add(moved);
        }

        return moved;
    }

    // A file a fixer reads (the repository's names), written again with the repository's paths.
    private static string FileIn(string path, Func<string, string> map, List<string> written)
    {
        if (!File.Exists(path))
        {
            return map(path);
        }

        var moved = Path.Combine(Path.GetTempPath(), $"stylebro-names-{Guid.NewGuid():N}.tsv");
        File.WriteAllText(moved, map(File.ReadAllText(path)));
        written.Add(moved);
        return moved;
    }

    /// <summary>A copy whose runs use the repository's host (<see cref="CopyHost"/>): both folders end in a separator.</summary>
    private sealed class CopyMap(string copy, string source)
    {
        private readonly List<FormatHost> hosts = [];

        public string Copy { get; } = copy;

        public string Source { get; } = source;

        /// <summary>Remembers a host the copy's runs used, to <see cref="Forget"/> them there.</summary>
        public void Used(FormatHost host)
        {
            lock (this.hosts)
            {
                if (!this.hosts.Contains(host))
                {
                    this.hosts.Add(host);
                }
            }
        }

        /// <summary>Drops what the hosts kept for the copy.</summary>
        public void Forget()
        {
            lock (this.hosts)
            {
                this.hosts.ForEach(h => h.Forget(this.Copy));
                this.hosts.Clear();
            }
        }
    }

    private sealed class EngineScope(Action end) : IDisposable
    {
        public void Dispose() => end();
    }
}
