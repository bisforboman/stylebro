using System;
using System.Collections.Generic;
using System.Linq;

namespace StyleBro.Migrate;

/// <summary>
/// Phase 1 of docs/proposals/format-speed.md (owner's decisions 2026-10-17): the checks run in an in-process host
/// (<see cref="FormatHost"/>) with --engine host; fixes still run in 'dotnet format' processes, and without the option
/// everything does, as before.
/// </summary>
internal static partial class FormatCommand
{
    /// <summary>Picks what runs the checks: <see cref="HostEngine"/> or <see cref="DotnetFormatEngine"/> (the default).</summary>
    public const string EngineOption = "--engine";

    /// <summary>The in-process host (experimental).</summary>
    public const string HostEngine = "host";

    /// <summary>A 'dotnet format' process per run and pass.</summary>
    public const string DotnetFormatEngine = "dotnet-format";

    private static readonly System.Threading.AsyncLocal<string?> Engine = new();

    /// <summary>
    /// Takes --engine out of <paramref name="args"/>: the engine it names, else <see cref="DotnetFormatEngine"/>; null and
    /// <paramref name="error"/> for a value that is neither.
    /// </summary>
    public static string? TakeEngine(ref string[] args, out string? error)
    {
        error = null;
        var at = Array.IndexOf(args, EngineOption);
        if (at < 0)
        {
            return DotnetFormatEngine;
        }

        var value = at + 1 < args.Length ? args[at + 1] : null;
        if (value is not (HostEngine or DotnetFormatEngine))
        {
            error = $"{EngineOption} takes '{HostEngine}' (experimental: the checks run in this process) or '{DotnetFormatEngine}' (the default).";
            return null;
        }

        args = args.Take(at).Concat(args.Skip(at + 2)).ToArray();
        return value;
    }

    /// <summary>Uses <paramref name="engine"/> for this command (an async-local, like <see cref="PhaseTimes"/>) until disposed.</summary>
    public static IDisposable UseEngine(string engine)
    {
        var before = Engine.Value;
        Engine.Value = engine;
        return new EngineScope(() => Engine.Value = before);
    }

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
    /// The host of a 'format' or 'check' command with --engine host, opened at the first check and closed with the command run;
    /// null with the default engine. A host that can't run here says why once and runs nothing.
    /// </summary>
    private sealed class HostScope(string root, Action<string> log) : IDisposable
    {
        private readonly object gate = new();
        private bool opened;
        private FormatHost? host;

        public static HostScope? For(string root, Action<string> log) => Engine.Value == HostEngine ? new HostScope(root, log) : null;

        /// <summary>
        /// Runs a check in the host (<see cref="FormatHost.Run"/>), or 'dotnet format' (<paramref name="otherwise"/>) when the
        /// host can't run here or doesn't run that command.
        /// </summary>
        public int Run(string workspace, RunEnvironment? run, IEnumerable<string> selections, IEnumerable<string> arguments, Action<string> output, Func<int> otherwise)
        {
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

            return open?.Run(workspace, root, HostEnvironment(run), selections, arguments, output) ?? otherwise();
        }

        public void Dispose() => this.host?.Dispose();
    }

    private sealed class EngineScope(Action end) : IDisposable
    {
        public void Dispose() => end();
    }
}
