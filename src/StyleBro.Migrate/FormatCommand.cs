using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// stylebro-migrate format [folder, solution or project] [--all] [dotnet format options]: 'dotnet format' that fixes
/// only StyleBro's rules and the built-in ones init/migrate turn on (<see cref="Diagnostics"/>; --all: everything), never
/// inside git submodules, and is safe in multi-targeted repositories. There 'dotnet format' loads one copy of every
/// file per target framework, the SDK's IDE0055 fix edits each copy on its own (each sees other '#if' code), and
/// Roslyn's linked-file merge crashes on
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

        // By default only StyleBro's rules and the built-in ones init/migrate turn on: plain 'dotnet format' also applies
        // every other analyzer's fixes and compiler fixes (CS8618's 'required', a Sonar fix removing 'init;': build
        // errors in Kavita). Whitespace formatting still runs. --all keeps them.
        if (!passThrough.Remove("--all") && !passThrough.Contains("--diagnostics"))
        {
            var ids = Diagnostics(root);
            Console.WriteLine($"Fixing StyleBro's rules and the built-in rules stylebro-migrate turns on ({ids.Count} ids); --all applies every analyzer's and compiler fix.");
            passThrough.Add("--diagnostics");
            passThrough.AddRange(ids);
        }

        // Submodules are someone else's code, even when a project compiles files from them.
        if (StyleCopSetup.NestedRepositories(root) is { Count: > 0 } submodules)
        {
            var exclude = passThrough.IndexOf("--exclude");
            if (exclude < 0)
            {
                passThrough.Add("--exclude");
                passThrough.AddRange(submodules);
            }
            else
            {
                passThrough.InsertRange(exclude + 1, submodules);
            }
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

        // The restore's own output (its "Build succeeded" block) only matters when it fails.
        var restoreOutput = new List<string>();
        var restore = Dotnet(root, null, new[] { "restore", workspacePath }, restoreOutput.Add);
        if (restore != 0)
        {
            restoreOutput.ForEach(Console.WriteLine);
            return restore;
        }

        // Every run would write its own format-report.json over the last one: each gets a folder, merged at the end.
        var reportIndex = passThrough.IndexOf("--report");
        var report = reportIndex >= 0 && reportIndex + 1 < passThrough.Count ? passThrough[reportIndex + 1] : null;
        var reportFolders = new List<string>();

        // The runs print the same diagnostics and workspace warnings once per framework: each line is shown once.
        var shown = new HashSet<string>(StringComparer.Ordinal);
        void Show(string line)
        {
            if (IsNewLine(shown, line))
            {
                Console.WriteLine(line);
            }
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

                    var options = passThrough.ToList();
                    if (report is not null)
                    {
                        var folder = Path.Combine(Path.GetTempPath(), $"stylebro-format-report-{Guid.NewGuid():N}");
                        reportFolders.Add(folder);
                        options[reportIndex + 1] = folder;
                    }

                    var arguments = new[] { "format", filter ?? workspacePath, "--no-restore" }.Concat(options);
                    if (!passThrough.Contains("--include"))
                    {
                        arguments = arguments.Append("--include").Concat(Include(root, solutionDirectory, selected));
                    }

                    var keep = Keep(projects.Values.ToDictionary(p => p.FullPath, p => (p.Frameworks, p.References), StringComparer.OrdinalIgnoreCase), framework);
                    var code = Dotnet(root, (framework, select, "|" + string.Join("|", keep.Select(k => k.ToUpperInvariant())) + "|"), arguments, Show);
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
            if (report is not null)
            {
                WriteMergedReport(Path.GetFullPath(report), reportFolders);
            }
        }

        return exit;
    }

    /// <summary>
    /// The ids 'stylebro-migrate format' fixes by default: every StyleBro rule, and the built-in IDE rules init's template
    /// and the stylebro blocks of the .editorconfig files at and above <paramref name="root"/> name (rules set to none
    /// aren't fixed anyway).
    /// </summary>
    public static List<string> Diagnostics(string root)
    {
        var ids = new SortedSet<string>(Migration.StyleBroRules().Select(r => r.Id), StringComparer.Ordinal);
        var text = new List<string> { InitCommand.Template() };
        for (var folder = new DirectoryInfo(root); folder is not null; folder = folder.Parent)
        {
            var path = Path.Combine(folder.FullName, ".editorconfig");
            if (File.Exists(path))
            {
                text.AddRange(Regex.Matches(File.ReadAllText(path), "# BEGIN stylebro-.*?# END stylebro-", RegexOptions.Singleline).Select(m => m.Value));
            }
        }

        foreach (Match match in Regex.Matches(string.Join("\n", text), @"dotnet_diagnostic\.(IDE\d+)\.severity", RegexOptions.IgnoreCase))
        {
            ids.Add(match.Groups[1].Value.ToUpperInvariant());
        }

        return ids.ToList();
    }

    /// <summary>
    /// format-report.json files merged: one entry per file (the first run's), with every distinct change of all runs
    /// (a change both frameworks see is listed once), in the order they came.
    /// </summary>
    public static string MergeReports(IEnumerable<string> reports)
    {
        var merged = new JsonArray();
        var byPath = new Dictionary<string, (JsonArray Changes, HashSet<string> Seen)>(StringComparer.OrdinalIgnoreCase);
        foreach (var report in reports)
        {
            foreach (var document in JsonNode.Parse(report)?.AsArray() ?? new JsonArray())
            {
                if (document is not JsonObject entry)
                {
                    continue;
                }

                var path = entry["FilePath"]?.GetValue<string>() ?? entry["FileName"]?.GetValue<string>() ?? string.Empty;
                var changes = entry["FileChanges"]?.AsArray() ?? new JsonArray();
                if (!byPath.TryGetValue(path, out var target))
                {
                    var copy = (JsonObject)entry.DeepClone();
                    copy["FileChanges"] = target.Changes = new JsonArray();
                    target.Seen = new HashSet<string>(StringComparer.Ordinal);
                    byPath[path] = target;
                    merged.Add(copy);
                }

                foreach (var change in changes)
                {
                    if (change is not null && target.Seen.Add(change.ToJsonString()))
                    {
                        target.Changes.Add(change.DeepClone());
                    }
                }
            }
        }

        return merged.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Whether an output line is shown: not when it was already (a diagnostic every framework's run reports, the
    /// workspace warning); blank lines always are.
    /// </summary>
    public static bool IsNewLine(ISet<string> shown, string line) => line.Trim().Length == 0 || shown.Add(line);

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

    /// <summary>
    /// Merges the runs' format-report.json files into the one 'dotnet format --report' would write: a path with an
    /// extension is the file, otherwise a folder that gets format-report.json. The temporary folders are deleted.
    /// </summary>
    private static void WriteMergedReport(string report, IEnumerable<string> folders)
    {
        var reports = new List<string>();
        foreach (var folder in folders)
        {
            var file = Path.Combine(folder, "format-report.json");
            if (File.Exists(file))
            {
                reports.Add(File.ReadAllText(file));
            }

            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        var path = Path.HasExtension(report) ? report : Path.Combine(report, "format-report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, MergeReports(reports));
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

    /// <summary>Runs dotnet; with <paramref name="output"/>, its output (stdout and stderr) goes there line by line instead of the console.</summary>
    private static int Dotnet(string directory, (string Name, string SelectTargets, string Keep)? framework, IEnumerable<string> arguments, Action<string>? output = null)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = directory, RedirectStandardOutput = output is not null, RedirectStandardError = output is not null };
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

        using var process = new Process { StartInfo = start };
        if (output is not null)
        {
            var gate = new object();
            void Forward(object sender, DataReceivedEventArgs e)
            {
                if (e.Data is not null)
                {
                    lock (gate)
                    {
                        output(e.Data);
                    }
                }
            }

            process.OutputDataReceived += Forward;
            process.ErrorDataReceived += Forward;
        }

        process.Start();
        if (output is not null)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}
