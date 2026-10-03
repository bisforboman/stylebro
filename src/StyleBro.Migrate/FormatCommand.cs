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
/// for one framework at a time (TargetFramework as an environment variable) leaves nothing to merge, so this runs
/// 'dotnet format' once per target framework, each time on the projects that target it (a temporary solution filter).
/// A repository without multi-targeted projects gets one plain run.
/// </summary>
internal static class FormatCommand
{
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
        var frameworks = ReadFrameworks(workspacePath);
        var plan = Plan(frameworks);
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
        var exit = 0;
        foreach (var (framework, projects) in plan)
        {
            Console.WriteLine($"== {framework} ({projects.Count} project(s))");
            var filter = isSolution ? Path.Combine(Path.GetDirectoryName(workspacePath)!, $".stylebro-format-{framework}.slnf") : null;
            try
            {
                if (filter is not null)
                {
                    File.WriteAllText(filter, SolutionFilter(Path.GetFileName(workspacePath), projects));
                }

                var code = Dotnet(root, framework, new[] { "format", filter ?? workspacePath, "--no-restore" }.Concat(passThrough));
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

    /// <summary>A solution filter (.slnf) next to the solution, with the given projects (paths as the solution lists them).</summary>
    public static string SolutionFilter(string solutionFileName, IEnumerable<string> projects)
    {
        return JsonSerializer.Serialize(
            new { solution = new { path = solutionFileName, projects = projects.ToArray() } },
            new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>The target frameworks of each project: 'TargetFrameworks' split, else 'TargetFramework'.</summary>
    public static string[] ParseFrameworks(string msbuildJson)
    {
        using var json = JsonDocument.Parse(msbuildJson);
        var properties = json.RootElement.GetProperty("Properties");
        var several = properties.TryGetProperty("TargetFrameworks", out var s) ? s.GetString() : null;
        var one = properties.TryGetProperty("TargetFramework", out var o) ? o.GetString() : null;
        return (string.IsNullOrWhiteSpace(several) ? one ?? string.Empty : several)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Each C# project of the solution (paths relative to the solution, as it lists them) or the project itself.</summary>
    private static Dictionary<string, string[]> ReadFrameworks(string workspacePath)
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
            p => ParseFrameworks(Capture(directory, "msbuild", Path.Combine(directory, p), "-getProperty:TargetFrameworks", "-getProperty:TargetFramework", "-nologo")));
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

    private static int Dotnet(string directory, string? targetFramework, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = directory };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        // An environment variable, not '-p': MSBuild then loads each project as an inner build for this framework.
        if (targetFramework is not null)
        {
            start.Environment["TargetFramework"] = targetFramework;
        }

        using var process = Process.Start(start)!;
        process.WaitForExit();
        return process.ExitCode;
    }
}
