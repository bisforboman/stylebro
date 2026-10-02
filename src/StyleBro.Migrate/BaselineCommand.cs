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
    public static int Run(string[] args)
    {
        var projectIndex = Array.IndexOf(args, "--project");
        var project = projectIndex >= 0 && projectIndex + 1 < args.Length ? args[projectIndex + 1] : null;
        var root = Path.GetFullPath(args.Where((a, i) => !a.StartsWith("--", StringComparison.Ordinal) && (projectIndex < 0 || i != projectIndex + 1)).FirstOrDefault() ?? ".");
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Not a directory: {root}");
            return 1;
        }

        project ??= FindWorkspace(root);
        if (project is null)
        {
            Console.Error.WriteLine($"No single solution or project in {root}; name one with --project.");
            return 1;
        }

        var path = Path.Combine(root, Baseline.FileName);
        var aside = path + ".previous";
        var reportDir = Path.Combine(Path.GetTempPath(), "stylebro-baseline-" + Guid.NewGuid().ToString("N"));
        if (File.Exists(path))
        {
            File.Move(path, aside, overwrite: true);
        }

        try
        {
            Console.WriteLine($"Running 'dotnet format {project} --verify-no-changes' to find today's violations...");
            Directory.CreateDirectory(reportDir);
            var exit = RunDotnetFormat(root, project, reportDir);
            var report = Path.Combine(reportDir, "format-report.json");
            if (!File.Exists(report))
            {
                Console.Error.WriteLine($"'dotnet format' wrote no report (exit code {exit}).");
                RestoreAside(path, aside);
                return 1;
            }

            var result = Build(root, File.ReadAllText(report));
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
            if (Directory.Exists(reportDir))
            {
                Directory.Delete(reportDir, recursive: true);
            }
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
            foreach (var change in document.GetProperty("FileChanges").EnumerateArray())
            {
                var id = change.GetProperty("DiagnosticId").GetString() ?? string.Empty;
                var line = change.GetProperty("LineNumber").GetInt32();
                if (!seen.Add((id, file, line, change.GetProperty("CharNumber").GetInt32())))
                {
                    continue;
                }

                if (id == "WHITESPACE")
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
            Console.WriteLine($"Not in the baseline: {result.Whitespace} whitespace changes ('dotnet format whitespace' formats without diagnostics, "
                + "so nothing can hide them). Run 'dotnet format whitespace' once, or check only 'dotnet format style' and 'dotnet format analyzers' in CI.");
        }

        foreach (var (id, count) in result.NotCovered.OrderBy(r => r.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"Not in the baseline: {count} {id} (not a StyleBro rule or one of the SDK rules the preset turns on).");
        }
    }

    private static int RunDotnetFormat(string root, string project, string reportDir)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "format", project, "--verify-no-changes", "--severity", "warn", "--report", reportDir })
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

    private static string? FindWorkspace(string root)
    {
        foreach (var pattern in new[] { "*.slnx", "*.sln", "*.csproj" })
        {
            var found = Directory.GetFiles(root, pattern);
            if (found.Length == 1)
            {
                return Path.GetFileName(found[0]);
            }

            if (found.Length > 1)
            {
                return null;
            }
        }

        return null;
    }

    private static void RestoreAside(string path, string aside)
    {
        if (File.Exists(aside) && !File.Exists(path))
        {
            File.Move(aside, path);
        }
    }
}
