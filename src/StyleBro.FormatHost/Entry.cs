using System;

namespace StyleBro.Host;

/// <summary>
/// What bro calls (by reflection, across load contexts: plain types only). <see cref="Open"/> starts a host for one bro
/// command, <see cref="Run"/> runs one 'dotnet format' check on it, <see cref="Close"/> ends it.
/// </summary>
public static class Entry
{
    /// <summary>What <see cref="Run"/> returns for a command the host doesn't run: bro starts 'dotnet format' for it.</summary>
    public const int Declined = -1;

    /// <summary>A host for one command; <paramref name="measure"/> times its phases (bro's --timings).</summary>
    public static object Open(Func<string, IDisposable> measure) => new Session(measure);

    /// <summary>
    /// Runs 'dotnet format <paramref name="arguments"/>' as a check on <paramref name="workspace"/>'s projects in
    /// <paramref name="environment"/> ('Name=value': the configuration and bro's hook), in <paramref name="folder"/>.
    /// <paramref name="selections"/>: the plan entry's projects, 'path|framework' (empty: the whole workspace). Its output
    /// goes to <paramref name="output"/> line by line; returns its exit code or <see cref="Declined"/>.
    /// </summary>
    public static int Run(object session, string workspace, string folder, string[] environment, string[] selections, string[] arguments, Action<string> output) =>
        ((Session)session).Run(workspace, folder, environment, selections, arguments, output);

    /// <summary>Ends a host: its workspaces are disposed.</summary>
    public static void Close(object session) => ((Session)session).Dispose();
}
