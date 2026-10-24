using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// The in-process engine of 'bro format' (--engine host, docs/proposals/format-speed.md, phases 1 and 2: checks and fixes). It loads
/// StyleBro.FormatHost into a load context of its own that resolves Roslyn, MSBuild and dotnet-format from the SDK the
/// repository's global.json selects (owner's decision 2026-10-17), next to the Roslyn 4.8 the tool itself uses; the two
/// never share a type, so everything crossing over is a string, an array of them, a delegate or an exit code. One host per
/// bro command run: it loads each configuration's workspace once for all the checks of the run.
/// </summary>
internal sealed class FormatHost : IDisposable
{
    // Set in the command run again on the newest runtime (RollForward), to DOTNET_ROLL_FORWARD's value before: it doesn't do
    // it again, and puts that value back.
    private const string RolledVariable = "STYLEBRO_ROLLED_FORWARD";

    private const string RollForwardVariable = "DOTNET_ROLL_FORWARD";

    // The host's Entry.LoadFailedMarker (another load context: plain strings only).
    private const string LoadFailedMarker = "stylebro-host-load-failed: ";

    private readonly MethodInfo run;
    private readonly MethodInfo runCopy;
    private readonly MethodInfo forget;
    private readonly MethodInfo close;
    private readonly object session;

    private FormatHost(Assembly host)
    {
        var entry = host.GetType("StyleBro.Host.Entry", throwOnError: true)!;
        this.run = entry.GetMethod("Run")!;
        this.runCopy = entry.GetMethod("RunCopy")!;
        this.forget = entry.GetMethod("Forget")!;
        this.close = entry.GetMethod("Close")!;
        Func<string, IDisposable> measure = FormatCommand.PhaseTimes.Measure;
        this.session = entry.GetMethod("Open")!.Invoke(null, [measure])!;
    }

    /// <summary>
    /// A host for the SDK that 'dotnet' resolves in <paramref name="root"/>, or null when it can't run here (said once through
    /// <paramref name="log"/>): bro then starts 'dotnet format' as before.
    /// </summary>
    public static FormatHost? Open(string root, Action<string> log)
    {
        try
        {
            var folder = FormatFolder(root, out var reason);
            if (folder is null)
            {
                log(FallbackLine(reason));
                return null;
            }

            // The SDK's dotnet-format targets that SDK's runtime: this process needs at least it (RollForward=Major picks the newest).
            if (RuntimeMajor(Path.Combine(folder, "dotnet-format.runtimeconfig.json")) is { } major && Environment.Version.Major < major)
            {
                log(FallbackLine($"the SDK's dotnet-format needs .NET {major}, and 'bro' runs on .NET {Environment.Version.Major}"));
                return null;
            }

            var hostPath = Path.Combine(AppContext.BaseDirectory, "StyleBro.FormatHost.dll");
            if (!File.Exists(hostPath))
            {
                log(FallbackLine($"{hostPath} is missing"));
                return null;
            }

            var context = new SdkLoadContext(folder);
            return new FormatHost(context.LoadFromAssemblyPath(hostPath));
        }
        catch (Exception exception)
        {
            log(FallbackLine((exception.InnerException ?? exception).Message));
            return null;
        }
    }

    /// <summary>
    /// With --engine host on a runtime older than the newest one installed, runs this command again on the newest
    /// (DOTNET_ROLL_FORWARD=LatestMajor) in the same console and returns its exit code; else null. The tool's
    /// RollForward=Major only moves to a newer runtime when its own (.NET 8) is missing, and the SDK's dotnet-format needs
    /// the SDK's runtime.
    /// </summary>
    public static int? RollForward(string[] args)
    {
        // The command run again: the runtime is chosen, the 'dotnet' processes it starts get the variable as it was.
        if (Environment.GetEnvironmentVariable(RolledVariable) is { } before)
        {
            Environment.SetEnvironmentVariable(RollForwardVariable, before.Length == 0 ? null : before);
            Environment.SetEnvironmentVariable(RolledVariable, null);
            return null;
        }

        var at = Array.IndexOf(args, FormatCommand.EngineOption);
        if (at < 0 || at + 1 >= args.Length || args[at + 1] != FormatCommand.HostEngine
            || NewestRuntime(Path.GetDirectoryName(typeof(object).Assembly.Location)) is not { } newest || newest <= Environment.Version.Major
            || Environment.ProcessPath is not { } process)
        {
            return null;
        }

        var start = new System.Diagnostics.ProcessStartInfo(process);
        if (Path.GetFileNameWithoutExtension(process).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            start.ArgumentList.Add(typeof(FormatHost).Assembly.Location);
        }

        args.ToList().ForEach(start.ArgumentList.Add);
        start.Environment[RolledVariable] = Environment.GetEnvironmentVariable(RollForwardVariable) ?? string.Empty;
        start.Environment[RollForwardVariable] = "LatestMajor";
        using var child = System.Diagnostics.Process.Start(start)!;
        child.WaitForExit();
        return child.ExitCode;
    }

    /// <summary>The highest major version among the runtimes next to <paramref name="runtimeFolder"/> (shared/Microsoft.NETCore.App/8.0.32), or null.</summary>
    public static int? NewestRuntime(string? runtimeFolder) =>
        Path.GetDirectoryName(runtimeFolder) is { } shared && Directory.Exists(shared)
            ? Directory.GetDirectories(shared).Select(d => Version.TryParse(Regex.Replace(Path.GetFileName(d), "-.*$", string.Empty), out var v) ? v.Major : (int?)null).Max()
            : null;

    /// <summary>What to say when the host can't run: 'dotnet format' runs the checks and fixes instead.</summary>
    public static string FallbackLine(string reason) => $"Engine: the host can't run here ({reason.TrimEnd('.')}), so 'dotnet format' runs the checks and fixes.";

    /// <summary>
    /// The SDK's dotnet-format folder for the SDK 'dotnet --version' names in <paramref name="root"/>, from 'dotnet --list-sdks',
    /// else null and why.
    /// </summary>
    public static string? FormatFolder(string root, out string reason)
    {
        var version = FormatCommand.Capture(root, "--version").Trim();
        var sdks = FormatCommand.Capture(root, "--list-sdks");
        var folder = SdkFolder(version, sdks) is { } sdk ? Path.Combine(sdk, "DotnetTools", "dotnet-format") : null;
        reason = folder is null ? $"no SDK folder for '{version}' in 'dotnet --list-sdks'" : FolderProblem(folder, version) ?? string.Empty;
        return reason.Length == 0 ? folder : null;
    }

    /// <summary>
    /// Why the host can't run the dotnet-format in <paramref name="folder"/> (SDK <paramref name="version"/>), or null. The
    /// host loads the projects with that folder's MSBuild workspace and its own Microsoft.Build; SDK 8's dotnet-format has
    /// none (it finds the SDK's MSBuild through MSBuildLocator), and every host run crashed there (trial 49, Cocona's
    /// global.json selects SDK 8).
    /// </summary>
    public static string? FolderProblem(string folder, string version) =>
        !File.Exists(Path.Combine(folder, "dotnet-format.dll")) ? $"the SDK {version} has no {folder}/dotnet-format.dll"
        : !File.Exists(Path.Combine(folder, "Microsoft.Build.dll")) ? $"the SDK {version}'s dotnet-format loads MSBuild through MSBuildLocator, which the host doesn't support"
        : null;

    /// <summary>The folder of SDK <paramref name="version"/> in 'dotnet --list-sdks' output ('10.0.113 [/usr/lib/dotnet/sdk]'), or null.</summary>
    public static string? SdkFolder(string version, string listSdks) =>
        listSdks.Split('\n').Select(l => Regex.Match(l.Trim(), @"^(?<version>\S+) \[(?<path>.+)\]$"))
            .Where(m => m.Success && m.Groups["version"].Value == version)
            .Select(m => Path.Combine(m.Groups["path"].Value, version))
            .FirstOrDefault();

    /// <summary>The major version of the runtime a runtimeconfig.json names, or null.</summary>
    public static int? RuntimeMajor(string runtimeConfig)
    {
        if (!File.Exists(runtimeConfig))
        {
            return null;
        }

        using var json = JsonDocument.Parse(File.ReadAllText(runtimeConfig));
        var options = json.RootElement.GetProperty("runtimeOptions");
        var framework = options.TryGetProperty("framework", out var single) ? single
            : options.TryGetProperty("frameworks", out var many) && many.GetArrayLength() > 0 ? many[0]
            : default;
        return framework.ValueKind == JsonValueKind.Object && framework.TryGetProperty("version", out var version) && Version.TryParse(version.GetString(), out var parsed) ? parsed.Major : null;
    }

    /// <summary>
    /// Whether the SDK's load context refuses <paramref name="name"/>: an assembly of the tool's own (next to it in
    /// <paramref name="toolFolder"/>, not in the SDK's <paramref name="formatFolder"/>). Falling back to the default context
    /// would hand them out: Roslyn's analyzer loader asks the compiler's context first, and got the tool's StyleBro.Analyzers
    /// (built on its Roslyn 4.8), whose analyzers then didn't fit the SDK's Roslyn and nothing was reported.
    /// </summary>
    public static bool Refuses(string name, string formatFolder, string toolFolder) =>
        !File.Exists(Path.Combine(formatFolder, name + ".dll")) && File.Exists(Path.Combine(toolFolder, name + ".dll"));

    /// <summary>
    /// Whether the shared framework in <paramref name="frameworkFolder"/> has <paramref name="name"/> at the version of the
    /// copy in <paramref name="formatFolder"/> or newer, as the .NET host decides between an app's copy and the framework's.
    /// The default context then hands out the framework's, the one the framework's other assemblies use: SDK 8's
    /// dotnet-format ships System.Collections.Immutable 8.0, and on .NET 10 the host loaded it next to the framework's
    /// System.Reflection.Metadata 10.0, whose ImmutableArray is the framework's, so Roslyn's MEF parts failed to load (trial 49,
    /// Cocona's global.json selects SDK 8).
    /// </summary>
    public static bool FromFramework(string name, string formatFolder, string frameworkFolder)
    {
        var framework = Path.Combine(frameworkFolder, name + ".dll");
        var own = Path.Combine(formatFolder, name + ".dll");
        return File.Exists(framework) && File.Exists(own) && AssemblyName.GetAssemblyName(framework).Version >= AssemblyName.GetAssemblyName(own).Version;
    }

    /// <summary>What to say when the host's workspace didn't load: 'dotnet format' runs the rest of the command.</summary>
    public static string LoadFailedLine(string reason) => $"Engine: the host couldn't load the workspace ({reason.TrimEnd('.')}), so 'dotnet format' runs the checks and fixes.";

    /// <summary>
    /// Runs 'dotnet <paramref name="arguments"/>' (a 'format' check or fix run) in the host, or returns null for a command it
    /// doesn't run (bro then starts it). <paramref name="environment"/>: what the child process would get that changes how
    /// MSBuild loads the projects; <paramref name="variables"/>: what else it would get, which the fixers read;
    /// <paramref name="selections"/>: the run's projects ('path|framework'), empty for the whole workspace.
    /// </summary>
    public int? Run(string workspace, string folder, IEnumerable<string> environment, IEnumerable<string> variables, IEnumerable<string> selections, IEnumerable<string> arguments, Action<string> output) =>
        this.Run(workspace, folder, environment, variables, selections, arguments, output, out _);

    /// <summary>
    /// <see cref="Run(string, string, IEnumerable{string}, IEnumerable{string}, IEnumerable{string}, IEnumerable{string}, Action{string})"/>,
    /// and why the workspace didn't load (<paramref name="loadFailure"/>, null when it did): bro then runs 'dotnet format'
    /// for the rest of the command.
    /// </summary>
    public int? Run(string workspace, string folder, IEnumerable<string> environment, IEnumerable<string> variables, IEnumerable<string> selections, IEnumerable<string> arguments, Action<string> output, out string? loadFailure) =>
        this.Run(null, workspace, folder, environment, variables, selections, arguments, output, out loadFailure);

    /// <summary>
    /// <see cref="Run(string, string, IEnumerable{string}, IEnumerable{string}, IEnumerable{string}, IEnumerable{string}, Action{string}, out string?)"/>
    /// for a run on <paramref name="copy"/> (folders with a separator at the end: the copy and the repository it was made of),
    /// in the repository's workspace: the paths given and printed are the repository's, the files are the copy's.
    /// </summary>
    public int? Run((string Copy, string Source)? copy, string workspace, string folder, IEnumerable<string> environment, IEnumerable<string> variables, IEnumerable<string> selections, IEnumerable<string> arguments, Action<string> output, out string? loadFailure)
    {
        loadFailure = null;
        var list = arguments.ToList();
        if (list.Count == 0 || list[0] != "format")
        {
            return null;
        }

        string? failure = null;
        void Output(string line)
        {
            if (line.StartsWith(LoadFailedMarker, StringComparison.Ordinal))
            {
                failure = line.Substring(LoadFailedMarker.Length);
                return;
            }

            output(line);
        }

        object[] parameters = [this.session, workspace, folder, environment.ToArray(), variables.ToArray(), selections.ToArray(), list.Skip(1).ToArray(), (Action<string>)Output];
        var code = copy is { } of
            ? (int)this.runCopy.Invoke(null, [this.session, of.Copy, of.Source, .. parameters.Skip(1)])!
            : (int)this.run.Invoke(null, parameters)!;
        loadFailure = failure;
        return code < 0 ? null : code;
    }

    /// <summary>Drops what the runs on <paramref name="copy"/> kept (its workspaces as the copy has the files).</summary>
    public void Forget(string copy) => this.forget.Invoke(null, [this.session, copy]);

    /// <inheritdoc/>
    public void Dispose() => this.close.Invoke(null, [this.session]);

    /// <summary>
    /// Resolves the SDK's dotnet-format folder first (its Roslyn, MSBuild, Microsoft.Extensions.*, System.CommandLine), then the
    /// shared framework, never the tool's own assemblies (<see cref="Refuses"/>): each project's analyzers load from its packages.
    /// A folder copy of a framework assembly that isn't newer than the framework's gives way to it (<see cref="FromFramework"/>).
    /// </summary>
    internal sealed class SdkLoadContext(string folder) : AssemblyLoadContext("StyleBro.FormatHost", isCollectible: false)
    {
        private static readonly string FrameworkFolder = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var path = Path.Combine(folder, assemblyName.Name + ".dll");
            return FromFramework(assemblyName.Name!, folder, FrameworkFolder) ? null
                : File.Exists(path) ? this.LoadFromAssemblyPath(path)
                : Refuses(assemblyName.Name!, folder, AppContext.BaseDirectory) ? throw new FileNotFoundException($"{assemblyName.Name} isn't the SDK's: not loaded in the format host.", assemblyName.Name)
                : null;
        }
    }
}
