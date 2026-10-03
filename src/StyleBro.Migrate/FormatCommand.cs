using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace StyleBro.Migrate;

/// <summary>
/// stylebro-migrate format [folder, solution or project] [dotnet format options]: 'dotnet format' that is
/// safe in multi-targeted repositories. There 'dotnet format' loads one copy of every file per target framework, the
/// SDK's IDE0055 fix edits each copy on its own (each sees other '#if' code), and Roslyn's linked-file merge crashes on
/// the result: nothing is written. Its whitespace pass only formats the first framework's code. Loading the projects
/// for one framework at a time leaves nothing to merge, so this runs 'dotnet format' once per target framework, each
/// time on the projects that target it (a temporary solution filter). A repository without multi-targeted projects
/// gets one plain run.
/// </summary>
/// <remarks>
/// The framework is chosen with TargetFramework as an environment variable: unlike a property set later, the project
/// file's own conditions ('$(TargetFramework)' == 'net46' for DefineConstants, references) see it. It reaches every
/// project the run loads, also referenced ones, so <see cref="SelectFrameworkTargets"/> clears it again, right after
/// the project file, for projects that should load as usual (<see cref="Keep"/>): forcing a library onto a framework it
/// doesn't have failed (Newtonsoft.Json's net46 tests: no net46 reference assemblies for the library, the tests were
/// skipped silently), and clearing it for every project without the framework failed too (Serilog's TestDummies,
/// netstandard2.0, references Serilog, which then offered only net8.0: TestDummies and the tests that use it didn't load).
/// </remarks>
internal static class FormatCommand
{
    /// <summary>
    /// Imported right after each project file (BeforeMicrosoftNETSdkTargets), before the SDK decides whether it's
    /// multi-targeted: a multi-targeted project not on the keep list loads as usual (Roslyn's per-framework global
    /// property then wins over the environment).
    /// </summary>
    public const string SelectFrameworkTargets = """
        <Project>
          <!-- stylebro-migrate format: TargetFramework comes from the environment; projects not on the keep list load as usual. -->
          <PropertyGroup Condition="'$(StyleBroFormatKeep)' != '' and '$(TargetFrameworks)' != '' and !$(StyleBroFormatKeep.Contains('|$(MSBuildProjectFullPath.ToUpperInvariant())|'))">
            <TargetFramework></TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    public static int Run(string[] args)
    {
        // Like 'dotnet format': an optional folder, solution or project first, then options, which pass through.
        var hasPath = args.Length > 0 && !args[0].StartsWith("-", StringComparison.Ordinal);
        var path = Path.GetFullPath(hasPath ? args[0] : ".");
        var passThrough = args.Skip(hasPath ? 1 : 0).ToList();
        var root = File.Exists(path) ? Path.GetDirectoryName(path)! : path;
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Not found: {path}");
            return 1;
        }

        var workspace = File.Exists(path) ? Path.GetFileName(path) : BaselineCommand.FindWorkspace(root);
        if (workspace is null)
        {
            Console.Error.WriteLine($"No single solution or project in {root}; name one.");
            return 1;
        }

        var workspacePath = Path.GetFullPath(Path.Combine(root, workspace));
        var solutionDirectory = Path.GetDirectoryName(workspacePath)!;
        var projects = ReadProjects(workspacePath);
        var plan = Plan(projects.ToDictionary(p => p.Key, p => p.Value.Frameworks));
        if (plan.Count <= 1)
        {
            return Dotnet(root, null, new[] { "format", workspacePath }.Concat(passThrough));
        }

        Console.WriteLine($"Multi-targeted: one 'dotnet format' run per target framework ({string.Join(", ", plan.Select(p => p.Framework))}).");
        var restore = Dotnet(root, null, new[] { "restore", workspacePath });
        if (restore != 0)
        {
            return restore;
        }

        var isSolution = !workspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
        var select = Path.Combine(Path.GetTempPath(), $"stylebro-format-{Guid.NewGuid():N}.targets");
        File.WriteAllText(select, SelectFrameworkTargets);
        var exit = 0;
        try
        {
            foreach (var (framework, selected) in plan)
            {
                Console.WriteLine($"== {framework} ({selected.Count} project(s))");
                var filter = isSolution ? Path.Combine(solutionDirectory, $".stylebro-format-{framework}.slnf") : null;
                try
                {
                    if (filter is not null)
                    {
                        File.WriteAllText(filter, SolutionFilter(Path.GetFileName(workspacePath), selected));
                    }

                    var arguments = new[] { "format", filter ?? workspacePath, "--no-restore" }.Concat(passThrough);
                    if (!passThrough.Contains("--include"))
                    {
                        arguments = arguments.Append("--include").Concat(Include(root, solutionDirectory, selected));
                    }

                    var keep = Keep(projects.Values.ToDictionary(p => p.FullPath, p => (p.Frameworks, p.References), StringComparer.OrdinalIgnoreCase), framework);
                    var code = Dotnet(root, (framework, select, "|" + string.Join("|", keep.Select(k => k.ToUpperInvariant())) + "|"), arguments);
                    if (exit == 0)
                    {
                        exit = code;
                    }
                }
                finally
                {
                    if (filter is not null)
                    {
                        File.Delete(filter);
                    }
                }
            }
        }
        finally
        {
            File.Delete(select);
        }

        return exit;
    }

    /// <summary>
    /// The runs: every target framework with the projects that target it, in a stable order. One entry (or none) means a
    /// plain run is enough: no project targets several frameworks.
    /// </summary>
    public static List<(string Framework, List<string> Projects)> Plan(IReadOnlyDictionary<string, string[]> frameworks)
    {
        if (frameworks.Values.All(f => f.Length <= 1))
        {
            return new List<(string, List<string>)>();
        }

        return frameworks
            .SelectMany(p => p.Value.Select(f => (Framework: f, Project: p.Key)))
            .GroupBy(x => x.Framework, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, g.Select(x => x.Project).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList()))
            .ToList();
    }

    /// <summary>
    /// The projects a run loads for its framework (by full path): those that target it, and those that reference one of
    /// them, directly or not, so the reference resolves (it only offers the run's framework then). The rest load as usual.
    /// </summary>
    public static HashSet<string> Keep(IReadOnlyDictionary<string, (string[] Frameworks, string[] References)> projects, string framework)
    {
        var keep = new HashSet<string>(
            projects.Where(p => p.Value.Frameworks.Contains(framework, StringComparer.OrdinalIgnoreCase)).Select(p => p.Key),
            StringComparer.OrdinalIgnoreCase);
        for (var added = true; added;)
        {
            added = false;
            foreach (var (project, info) in projects)
            {
                if (!keep.Contains(project) && info.References.Any(keep.Contains))
                {
                    added = keep.Add(project) || added;
                }
            }
        }

        return keep;
    }

    /// <summary>
    /// The folders of a run's projects, relative to <paramref name="root"/>, for 'dotnet format --include'. Referenced
    /// projects that load as usual (multi-targeted) would get their files formatted too, merging the frameworks' copies
    /// again (Newtonsoft.Json: conflict markers in the library during the tests' net6.0 run). Only the run's own projects
    /// are edited; the others get their own runs.
    /// </summary>
    public static IEnumerable<string> Include(string root, string solutionDirectory, IEnumerable<string> projects)
    {
        // A folder needs the trailing '/': 'src/Lib' matches nothing (silently), 'src/Lib/' its files.
        return projects
            .Select(p => Path.GetRelativePath(root, Path.GetDirectoryName(Path.GetFullPath(Path.Combine(solutionDirectory, p)))!))
            .Select(folder => folder == "." ? "**/*.cs" : folder.Replace(Path.DirectorySeparatorChar, '/') + "/")
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>A solution filter (.slnf) next to the solution, with the given projects (paths as the solution lists them).</summary>
    public static string SolutionFilter(string solutionFileName, IEnumerable<string> projects)
    {
        return JsonSerializer.Serialize(
            new { solution = new { path = solutionFileName, projects = projects.ToArray() } },
            new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>A project's target frameworks ('TargetFrameworks' split, else 'TargetFramework') and project references (full paths).</summary>
    public static (string[] Frameworks, string[] References) ParseProject(string msbuildJson)
    {
        using var json = JsonDocument.Parse(msbuildJson);
        var properties = json.RootElement.GetProperty("Properties");
        var several = properties.TryGetProperty("TargetFrameworks", out var s) ? s.GetString() : null;
        var one = properties.TryGetProperty("TargetFramework", out var o) ? o.GetString() : null;
        var frameworks = (string.IsNullOrWhiteSpace(several) ? one ?? string.Empty : several)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var references = json.RootElement.TryGetProperty("Items", out var items) && items.TryGetProperty("ProjectReference", out var refs)
            ? refs.EnumerateArray().Select(r => r.GetProperty("FullPath").GetString()!).ToArray()
            : Array.Empty<string>();
        return (frameworks, references);
    }

    /// <summary>Each C# project of the solution (keyed by its path as the solution lists it) or the project itself.</summary>
    private static Dictionary<string, (string FullPath, string[] Frameworks, string[] References)> ReadProjects(string workspacePath)
    {
        var directory = Path.GetDirectoryName(workspacePath)!;
        var projects = workspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? new[] { Path.GetFileName(workspacePath) }
            : Capture(directory, "sln", workspacePath, "list").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        return projects.ToDictionary(
            p => p,
            p =>
            {
                var fullPath = Path.GetFullPath(Path.Combine(directory, p));
                var (frameworks, references) = ParseProject(Capture(directory, "msbuild", fullPath, "-getProperty:TargetFrameworks", "-getProperty:TargetFramework", "-getItem:ProjectReference", "-nologo"));
                return (fullPath, frameworks, references);
            });
    }

    private static string Capture(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return output.Result;
    }

    private static int Dotnet(string directory, (string Name, string SelectTargets, string Keep)? framework, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = directory };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        // See the remarks on the class: the environment, cleared again for projects that should load as usual.
        if (framework is { } f)
        {
            start.Environment["TargetFramework"] = f.Name;
            start.Environment["StyleBroFormatKeep"] = f.Keep;
            start.Environment["BeforeMicrosoftNETSdkTargets"] = f.SelectTargets;
        }

        using var process = Process.Start(start)!;
        process.WaitForExit();
        return process.ExitCode;
    }
}
