using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using StyleBro.Analyzers.Baseline;

namespace StyleBro.Migrate;

/// <summary>
/// stylebro-migrate baseline [path] [--project &lt;solution or project&gt;]: writes stylebro.baseline at 'path' (default:
/// the current directory) with every violation 'dotnet format' would fix today, so the build and 'dotnet format' only
/// report new ones. Runs 'dotnet format --verify-no-changes --report' (with an existing baseline moved aside, so it
/// starts over) and fingerprints each reported line. Whitespace formatting isn't a diagnostic in 'dotnet format' and
/// can't be baselined; the summary counts it.
/// </summary>
internal static class BaselineCommand
{
    /// <summary>
    /// What 'dotnet format' reports for its formatting passes, not as diagnostics: whitespace, line endings, the charset
    /// (BOM), the final newline, using order. Nothing can suppress them, so they can't be baselined.
    /// </summary>
    /// <summary>Folders below the root never searched for a solution (build output, packages).</summary>
    private static readonly string[] SkippedFolders = { "bin", "obj", "node_modules", "artifacts", "packages" };

    private static readonly HashSet<string> FormattingIds = new(StringComparer.Ordinal) { "WHITESPACE", "ENDOFLINE", "CHARSET", "FINALNEWLINE", "IMPORTS" };

    public static int Run(string[] args)
    {
        var (root, project) = Arguments(args, Environment.CurrentDirectory);
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Not found: {root}");
            return 1;
        }

        if (project is null)
        {
            project = FindWorkspace(root);
            if (project is null)
            {
                Console.Error.WriteLine(NoWorkspace(root, "--project <file>"));
                return 1;
            }

            if (PickedNote(root, project) is { } note)
            {
                Console.WriteLine(note);
            }
        }

        // 'dotnet format' can't load a Traversal project: a solution with its projects instead.
        var traversal = FormatCommand.IsTraversal(Path.Combine(root, project)) ? FormatCommand.TraversalSolution(Path.GetFullPath(Path.Combine(root, project))) : null;
        project = traversal ?? project;

        var path = Path.Combine(root, Baseline.FileName);
        var aside = path + ".previous";
        var reportDir = Path.Combine(Path.GetTempPath(), "stylebro-baseline-" + Guid.NewGuid().ToString("N"));
        if (File.Exists(path))
        {
            File.Move(path, aside, overwrite: true);
        }

        try
        {
            // Where IDE0055 is off (CSharpier, column alignment), whitespace isn't StyleBro's: like 'format', only the style
            // and analyzer passes run (EntityGraphQL: 156 whitespace changes against CSharpier's layout were reported).
            // Where it's off in some folders only (CSharpier formats one), the whitespace pass runs on its own without them.
            var exclusions = FormatCommand.WhitespaceExclusions(root);
            var passes = FormatCommand.Passes(exclusions);
            Console.WriteLine($"Running 'dotnet format {(passes.Length > 1 ? string.Join("/", passes) + " " : string.Empty)}{project} --verify-no-changes' to find today's violations...");
            var reports = new List<string>();
            foreach (var pass in passes)
            {
                var folder = Path.Combine(reportDir, pass.Length == 0 ? "all" : pass);
                Directory.CreateDirectory(folder);
                var exit = RunDotnetFormat(root, project, folder, pass, exclusions);
                var report = Path.Combine(folder, "format-report.json");
                if (!File.Exists(report))
                {
                    Console.Error.WriteLine($"'dotnet format' wrote no report (exit code {exit}).");
                    RestoreAside(path, aside);
                    return 1;
                }

                reports.Add(File.ReadAllText(report));
            }

            var result = Build(root, MergeReports(reports));
            File.WriteAllText(path, result.Baseline.Render());
            File.Delete(aside);
            Report(result);
            return 0;
        }
        catch
        {
            RestoreAside(path, aside);
            throw;
        }
        finally
        {
            if (traversal is not null)
            {
                File.Delete(traversal);
            }

            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// The folder the baseline goes into and the solution or project ('--project', relative to it; null: pick one). Like
    /// format, a solution or project may come first ('baseline src/SourceGit.csproj'): the baseline then goes into
    /// <paramref name="current"/> when the file is below it (the build looks for it above every project), else next to it.
    /// </summary>
    public static (string Root, string? Project) Arguments(string[] args, string current)
    {
        var projectIndex = Array.IndexOf(args, "--project");
        var project = projectIndex >= 0 && projectIndex + 1 < args.Length ? args[projectIndex + 1] : null;
        var root = Path.GetFullPath(Path.Combine(current, args.Where((a, i) => !a.StartsWith("--", StringComparison.Ordinal) && (projectIndex < 0 || i != projectIndex + 1)).FirstOrDefault() ?? "."));
        if (project is not null || !File.Exists(root))
        {
            return (root, project);
        }

        var relative = Path.GetRelativePath(current, root);
        return relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)
            ? (Path.GetDirectoryName(root)!, Path.GetFileName(root))
            : (Path.GetFullPath(current), relative);
    }

    /// <summary>
    /// Turns a report into a baseline. A diagnostic reported for several target frameworks' copies of a file counts
    /// once (the suppressor checks each compilation on its own).
    /// </summary>
    public static Result Build(string root, string reportJson)
    {
        var covered = new HashSet<string>(new Analyzers.Baseline.BaselineSuppressor().SupportedSuppressions.Select(s => s.SuppressedDiagnosticId), StringComparer.Ordinal);
        var seen = new HashSet<(string Id, string File, int Line, int Char)>();
        var counts = new Dictionary<Baseline.Key, int>();
        var perRule = new Dictionary<string, int>(StringComparer.Ordinal);
        var notCovered = new Dictionary<string, int>(StringComparer.Ordinal);
        var whitespace = 0;
        var lines = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        using var report = JsonDocument.Parse(reportJson);
        foreach (var document in report.RootElement.EnumerateArray())
        {
            var file = document.GetProperty("FilePath").GetString() ?? string.Empty;

            // Build output (obj/: generated AssemblyInfo, global usings) isn't the repository's code.
            if (Path.GetRelativePath(root, file).Replace('\\', '/').Split('/').Any(f => f.Equals("obj", StringComparison.OrdinalIgnoreCase) || f.Equals("bin", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            foreach (var change in document.GetProperty("FileChanges").EnumerateArray())
            {
                var id = change.GetProperty("DiagnosticId").GetString() ?? string.Empty;
                var line = change.GetProperty("LineNumber").GetInt32();
                if (!seen.Add((id, file, line, change.GetProperty("CharNumber").GetInt32())))
                {
                    continue;
                }

                if (FormattingIds.Contains(id))
                {
                    whitespace++;
                    continue;
                }

                if (!covered.Contains(id) || Baseline.RelativePath(root, file) is not { } relative)
                {
                    notCovered[id] = notCovered.TryGetValue(id, out var n) ? n + 1 : 1;
                    continue;
                }

                if (!lines.TryGetValue(file, out var text))
                {
                    lines[file] = text = File.ReadAllText(file).Replace("\r\n", "\n").Split('\n');
                }

                var key = new Baseline.Key(id, relative, Baseline.Fingerprint(line >= 1 && line <= text.Length ? text[line - 1] : string.Empty));
                counts[key] = counts.TryGetValue(key, out var count) ? count + 1 : 1;
                perRule[id] = perRule.TryGetValue(id, out var r) ? r + 1 : 1;
            }
        }

        return new Result(new Baseline(counts), perRule, whitespace, notCovered);
    }

    /// <summary>The solution or project the commands run on when none is named: the only candidate in the folder, else null.</summary>
    public static string? FindWorkspace(string root) => WorkspaceCandidates(root) is [var only] ? only : null;

    /// <summary>
    /// What the commands could run on in a folder (paths relative to it): its solutions (.slnx, .sln, .slnf; a .sln next to
    /// the .slnx of the same name is that solution, not another), else its projects, else a Traversal project
    /// (Microsoft.Build.Traversal, kubernetes-client's root build), else the solutions one folder down (tfplan2md keeps its
    /// only one in src/; <see cref="IsBelow"/>). Several are never picked from silently: eShop's eShop.slnx next to
    /// eShop.Web.slnf (the slnx needs MAUI workloads), Ocelot's two .slnx files; a solution with several filters of it is
    /// that solution (<see cref="Solutions"/>).
    /// </summary>
    public static List<string> WorkspaceCandidates(string root)
    {
        var files = Directory.EnumerateFiles(root).Select(Path.GetFileName).OfType<string>().ToList();
        var here = Solutions(root, files);
        if (here.Count == 0)
        {
            here = files.Where(f => Is(f, ".csproj")).Order(StringComparer.OrdinalIgnoreCase).ToList();
        }

        if (here.Count == 0)
        {
            here = files.Where(f => Is(f, ".proj") && FormatCommand.IsTraversal(Path.Combine(root, f))).Order(StringComparer.OrdinalIgnoreCase).ToList();
        }

        return here.Count > 0
            ? here
            : Directory.EnumerateDirectories(root)
                .Where(d => !Path.GetFileName(d).StartsWith('.') && !SkippedFolders.Contains(Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
                .SelectMany(d => Solutions(d, Directory.EnumerateFiles(d).Select(Path.GetFileName).OfType<string>().ToList()).Select(f => Path.Combine(Path.GetFileName(d), f)))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    /// <summary>Whether a picked workspace (<see cref="FindWorkspace"/>) is in a folder below the root, not at it.</summary>
    public static bool IsBelow(string workspace) => Path.GetDirectoryName(workspace) is { Length: > 0 };

    /// <summary>What to say when the workspace was picked one folder down (<see cref="IsBelow"/>).</summary>
    public static string BelowNote(string workspace) => $"Using {workspace.Replace('\\', '/')}: no solution or project at the root, and it's the only solution one folder down.";

    // Not Directory.GetFiles(root, "*.sln"): on Windows a three-letter extension pattern also matches .slnx and .slnf.
    private static bool Is(string file, string extension) => string.Equals(Path.GetExtension(file), extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The solutions in a folder. One solution with several filters of it is that solution (Steeltoe: Steeltoe.All.slnx and
    /// seven .slnf parts of it); a single filter next to it stays a choice (eShop.Web.slnf leaves out the projects that need
    /// MAUI workloads).
    /// </summary>
    private static List<string> Solutions(string folder, List<string> files)
    {
        var solutions = files.Where(f => Is(f, ".slnx") || Is(f, ".slnf") || (Is(f, ".sln") && !files.Contains(Path.ChangeExtension(f, ".slnx"), StringComparer.OrdinalIgnoreCase)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var filters = solutions.Where(f => Is(f, ".slnf")).ToList();
        return solutions.Except(filters).ToList() is [var only] && filters.Count > 1 && filters.All(f => IsFilterOf(Path.Combine(folder, f), Path.Combine(folder, only)))
            ? [only]
            : solutions;
    }

    /// <summary>Whether a solution filter filters the solution (a .sln and .slnx of the same name are one solution).</summary>
    private static bool IsFilterOf(string filter, string solution) =>
        FormatCommand.SolutionFilterError(filter) is null
            && string.Equals(Path.ChangeExtension(FormatCommand.ReadSolutionFilter(filter).Solution, null), Path.ChangeExtension(Path.GetFullPath(solution), null), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// What to say about a workspace <see cref="FindWorkspace"/> picked: one folder down (<see cref="BelowNote"/>), and the
    /// solution filters of it next to it, which a run can name instead; null when there's nothing to say.
    /// </summary>
    public static string? PickedNote(string root, string workspace)
    {
        var folder = Path.GetDirectoryName(workspace) ?? string.Empty;
        var filters = Directory.EnumerateFiles(Path.Combine(root, folder)).Where(f => Is(f, ".slnf") && IsFilterOf(f, Path.Combine(root, workspace)))
            .Select(f => Path.Combine(folder, Path.GetFileName(f)).Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var filtersText = filters.Count == 0
            ? null
            : $"its solution filters ({string.Join(", ", filters.Take(5))}{(filters.Count > 5 ? $" and {filters.Count - 5} more" : string.Empty)}) are parts of it: name one to run on only its projects, e.g. 'stylebro-migrate format {filters[0]}'.";
        return (IsBelow(workspace), filtersText) switch
        {
            (true, null) => BelowNote(workspace),
            (true, { } text) => BelowNote(workspace).TrimEnd('.') + "; " + text,
            (false, { } text) => $"Using {workspace.Replace('\\', '/')}; {text}",
            _ => null,
        };
    }

    /// <summary>Why no solution or project was picked in <paramref name="root"/>, and how to name one (<paramref name="how"/>, e.g. "--project &lt;file&gt;").</summary>
    public static string NoWorkspace(string root, string how)
    {
        var candidates = WorkspaceCandidates(root);
        return candidates.Count == 0
            ? $"No solution or project in {root}; name one with {how}."
            : $"Several solutions or projects in {root}: {string.Join(", ", candidates)}. None is picked for you (a solution filter may leave projects out on purpose): name one, e.g. {string.Join(" or ", candidates.Take(3).Select(c => how.Replace("<file>", c)))}{(how.Contains("--project", StringComparison.Ordinal) ? string.Empty : " (--project <file> works too)")}.";
    }

    private static void Report(Result result)
    {
        var total = result.PerRule.Values.Sum();
        Console.WriteLine($"Wrote {Baseline.FileName}: {total} violations on {result.Baseline.Count} lines.");
        foreach (var (id, count) in result.PerRule.OrderByDescending(r => r.Value).ThenBy(r => r.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"  {id,-8} {count}");
        }

        if (result.Whitespace > 0)
        {
            Console.WriteLine($"Not in the baseline: {result.Whitespace} formatting changes (whitespace, line endings, charset, final newline or using order: "
                + "'dotnet format' makes them without diagnostics, so nothing can hide them). Run 'stylebro-migrate format' once, or check only "
                + "'dotnet format style' and 'dotnet format analyzers' in CI.");
        }

        foreach (var (id, count) in result.NotCovered.OrderBy(r => r.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"Not in the baseline: {count} {id} (not a StyleBro rule or one of the SDK rules 'stylebro-migrate init' turns on).");
        }
    }

    /// <summary>Several 'dotnet format' JSON reports (arrays of documents) as one.</summary>
    public static string MergeReports(IEnumerable<string> reports) =>
        new System.Text.Json.Nodes.JsonArray(reports.SelectMany(r => System.Text.Json.Nodes.JsonNode.Parse(r)!.AsArray().Select(d => d?.DeepClone())).ToArray()).ToJsonString();

    private static int RunDotnetFormat(string root, string project, string reportDir, string pass, IReadOnlyCollection<string>? exclusions)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
        var options = new List<string> { "--verify-no-changes", "--severity", "warn", "--report", reportDir };
        options = pass == "whitespace" ? FormatCommand.WhitespaceOptions(options, exclusions!) : options;
        foreach (var argument in new[] { "format", pass, project }.Where(a => a.Length > 0).Concat(options))
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return process.ExitCode;
    }

    private static void RestoreAside(string path, string aside)
    {
        if (File.Exists(aside) && !File.Exists(path))
        {
            File.Move(aside, path);
        }
    }

    /// <summary>The baseline for a 'dotnet format' JSON report, and what it couldn't include.</summary>
    public sealed class Result
    {
        public Result(Baseline baseline, Dictionary<string, int> perRule, int whitespace, Dictionary<string, int> notCovered)
        {
            Baseline = baseline;
            PerRule = perRule;
            Whitespace = whitespace;
            NotCovered = notCovered;
        }

        public Baseline Baseline { get; }

        public Dictionary<string, int> PerRule { get; }

        public int Whitespace { get; }

        public Dictionary<string, int> NotCovered { get; }
    }
}
