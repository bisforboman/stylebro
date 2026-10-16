using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// CSharpier after 'format' where the repository formats with it (owner's decision 2026-10-16, "Run CSharpier after format"):
/// 'format' runs it on the files it changed, at the end of each run, and '--verify-no-changes' checks its C# files with it too.
/// Before, a CI's 'dotnet csharpier check .' failed after a clean format (Dawnshard: 21 files, BRO1149's ')' placement, BRO1141's
/// line width, BRO1001's blank lines). The repository's own CSharpier: its dotnet tool (restored first), else the CSharpier.MsBuild
/// package's copy.
/// </summary>
internal static class CSharpierStep
{
    /// <summary>The longest file list one CSharpier call gets (Windows limits a command line to 32,767 characters).</summary>
    private const int MaxArgumentLength = 8000;

    /// <summary>The tool manifests restored in this process.</summary>
    private static readonly HashSet<string> Restored = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The repository's CSharpier for <paramref name="root"/>, or null when it doesn't format with CSharpier or can't run it
    /// (only a .csharpierrc, no tool or package: <see cref="FormatCommand.AfterNotes"/> says to run it then).
    /// </summary>
    public static Tool? Find(string root)
    {
        var scopes = Conventions.CSharpierScopes(root);
        if (scopes.Count == 0)
        {
            return null;
        }

        var folders = scopes.Select(s => s.Folder).ToList();

        // The tool: a manifest that names it, at a scope's evidence or above the root (dotnet finds manifests upward).
        var manifests = scopes.Select(s => Path.GetFullPath(Path.Combine(root, s.Evidence))).Where(f => Path.GetFileName(f).Equals("dotnet-tools.json", StringComparison.OrdinalIgnoreCase))
            .Concat(Above(root).SelectMany(f => new[] { Path.Combine(f, ".config", "dotnet-tools.json"), Path.Combine(f, "dotnet-tools.json") }));
        foreach (var manifest in manifests.Where(File.Exists))
        {
            if (ToolVersion(File.ReadAllText(manifest)) is { } version)
            {
                var folder = Path.GetDirectoryName(manifest)!;
                folder = Path.GetFileName(folder).Equals(".config", StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(folder)! : folder;
                return new Tool(folder, ["csharpier"], Major(version) >= 1, folders, manifest);
            }
        }

        // The CSharpier.MsBuild package carries CSharpier itself (its build runs 'dotnet exec .../CSharpier.dll').
        var build = string.Join("\n", StyleCopSetup.EnumerateFiles(FormatCommand.RepositoryRoot(root) ?? root).Where(StyleCopSetup.IsMSBuild).Select(File.ReadAllText));
        if (PackageVersion(build) is { } package && PackageDll(package) is { } dll)
        {
            return new Tool(Path.GetFullPath(root), ["exec", dll], Major(package) >= 1, folders, null);
        }

        return null;
    }

    /// <summary>The CSharpier version a tool manifest names, or null.</summary>
    public static string? ToolVersion(string manifest)
    {
        try
        {
            using var json = JsonDocument.Parse(manifest, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            return json.RootElement.TryGetProperty("tools", out var tools)
                ? tools.EnumerateObject().Where(t => t.Name.Equals("csharpier", StringComparison.OrdinalIgnoreCase)).Select(t => t.Value.TryGetProperty("version", out var v) ? v.GetString() : null).FirstOrDefault()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The CSharpier.MsBuild version the MSBuild files' text names (a PackageReference's or a central PackageVersion), or null.</summary>
    public static string? PackageVersion(string build) =>
        Regex.Match(build, @"<(?:PackageReference|PackageVersion|GlobalPackageReference)\b[^>]*\bInclude\s*=\s*""CSharpier\.MsBuild""[^>]*\bVersion\s*=\s*""(?<version>[^""]+)""", RegexOptions.IgnoreCase) is { Success: true } match
            ? match.Groups["version"].Value.Trim()
            : null;

    /// <summary>
    /// The arguments of the CSharpier calls that run <paramref name="verb"/> ('format' or 'check') on <paramref name="files"/>
    /// (relative to the tool's folder), as few calls as the command-line length allows. CSharpier before 1.0 has no verbs: it
    /// formats by default and checks with '--check'.
    /// </summary>
    public static List<string[]> Commands(Tool tool, string verb, IReadOnlyList<string> files)
    {
        string[] head = tool.Verbs ? [.. tool.Command, verb] : verb == "check" ? [.. tool.Command, "--check"] : tool.Command;
        if (tool.Manifest is null)
        {
            head = [.. head, "--no-msbuild-check"];
        }

        var commands = new List<string[]>();
        var batch = new List<string>();
        foreach (var file in files)
        {
            if (batch.Count > 0 && batch.Sum(f => f.Length + 3) + file.Length > MaxArgumentLength)
            {
                commands.Add([.. head, .. batch]);
                batch.Clear();
            }

            batch.Add(file);
        }

        if (batch.Count > 0)
        {
            commands.Add([.. head, .. batch]);
        }

        return commands;
    }

    /// <summary>The files of <paramref name="files"/> (full paths) CSharpier formats: C# files in one of its folders.</summary>
    public static List<string> InScope(Tool tool, string root, IEnumerable<string> files) =>
        files.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(f))
            .Where(f => Path.GetRelativePath(root, f).Replace('\\', '/') is var relative && !relative.StartsWith("../", StringComparison.Ordinal)
                && tool.Scopes.Any(s => s.Length == 0 || relative.StartsWith(s + "/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

    /// <summary>
    /// The files a run's fixes changed that CSharpier put back as they were before the run (<paramref name="before"/>, after the
    /// fixes <paramref name="fixedFiles"/>, then <paramref name="after"/>): the two formatters undo each other there (Dawnshard:
    /// IDE0036's 'static file' against CSharpier's 'file static'), so no run settles them.
    /// </summary>
    public static List<string> PutBack(IReadOnlyDictionary<string, string> before, IReadOnlyDictionary<string, string> fixedFiles, IReadOnlyDictionary<string, string> after) =>
        fixedFiles.Where(f => before.TryGetValue(f.Key, out var was) && was != f.Value && after.TryGetValue(f.Key, out var now) && now == was)
            .Select(f => f.Key)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>What format and verify say about <see cref="PutBack"/>'s files (relative paths).</summary>
    public static string PutBackLine(IReadOnlyList<string> files) =>
        $"Not clean: CSharpier puts back what the fixes changed in {string.Join(", ", files.Take(5))}{(files.Count > 5 ? $" and {files.Count - 5} more" : string.Empty)}, "
        + "so the two formatters undo each other there and no run settles it. Run 'stylebro-migrate init' again (with CSharpier it turns off the built-in rules CSharpier decides, "
        + "e.g. IDE0036's modifier order), or set the rule that changes those lines to none.";

    /// <summary>
    /// The files (full paths, any kind CSharpier checks) under the tool's folders that CSharpier would change now, from one
    /// 'check' of the folders (fast: a file named on the command line costs ~80 ms, a folder's ~1 s for hundreds of files), or
    /// null when the check failed otherwise. <see cref="Arguments"/> leaves their folders to explicit file names.
    /// </summary>
    public static HashSet<string>? Unformatted(Tool tool, string root)
    {
        var (code, output) = Run(tool, "check", Folders(tool, root));
        var failed = Failures(output).Select(f => Path.GetFullPath(Path.Combine(tool.Folder, f))).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return code == 0 || failed.Count > 0 ? failed : null;
    }

    /// <summary>
    /// What to give CSharpier for <paramref name="files"/> (full paths) when <paramref name="unformatted"/> (<see cref="Unformatted"/>)
    /// is known: for each file the highest folder up to its scope's that holds no unformatted file (all CSharpier would
    /// change there is ours), else the file itself; folders and files under another one given once. Without it, the files.
    /// </summary>
    public static List<string> Arguments(Tool tool, string root, IReadOnlyList<string> files, IReadOnlySet<string>? unformatted)
    {
        if (unformatted is null)
        {
            return files.ToList();
        }

        bool Clean(string folder) => !unformatted.Any(u => u.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        var top = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var chosen = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(top, file).Replace('\\', '/');
            var scope = tool.Scopes.Where(s => s.Length == 0 || relative.StartsWith(s + "/", StringComparison.OrdinalIgnoreCase)).OrderBy(s => s.Length).FirstOrDefault();
            var limit = scope is null ? Path.GetDirectoryName(file)! : scope.Length == 0 ? top : Path.Combine(top, scope.Replace('/', Path.DirectorySeparatorChar));

            // An unformatted file's own folders aren't clean: it goes alone.
            string? best = null;
            for (var folder = Path.GetDirectoryName(file); folder is not null && folder.Length >= limit.Length; folder = Path.GetDirectoryName(folder))
            {
                if (Clean(folder))
                {
                    best = folder;
                }
            }

            chosen.Add(best ?? file);
        }

        return chosen.Where(c => !chosen.Any(o => o != c && c.StartsWith(o + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    /// <summary>
    /// What a 'check' says about C# files: null when they're as CSharpier formats them (exit code 0, or only other files it
    /// would change: CSharpier 1.x checks XML files too, Speckle's nuget.config line endings), else the C# files it would
    /// change ('/'-separated, as it prints them), empty when it failed otherwise.
    /// </summary>
    public static List<string>? FailedCSharp(int code, IReadOnlyList<string> output)
    {
        var all = Failures(output);
        var failed = all.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).Select(f => f.Replace('\\', '/').TrimStart('.', '/')).ToList();
        return code == 0 || (failed.Count == 0 && all.Count > 0) ? null : failed;
    }

    /// <summary>The paths CSharpier's output says it would change ('Error ./src/A.cs - Was not formatted.'), as it prints them.</summary>
    public static List<string> Failures(IEnumerable<string> output) =>
        output.Select(l => Regex.Match(l, "^Error (?<file>.+?) - Was not formatted")).Where(m => m.Success).Select(m => m.Groups["file"].Value).Distinct().ToList();

    /// <summary>
    /// Runs CSharpier's <paramref name="verb"/> on <paramref name="files"/> (full paths of files or folders, files already
    /// <see cref="InScope"/>); the exit code (the first that isn't 0) and the output. Restores the tool first, once per process.
    /// </summary>
    public static (int Code, List<string> Output) Run(Tool tool, string verb, IReadOnlyList<string> files)
    {
        var output = new List<string>();
        if (tool.Manifest is not null && Restored.Add(tool.Folder))
        {
            var restore = FormatCommand.RunDotnet(tool.Folder, ["tool", "restore"], output.Add);
            if (restore != 0)
            {
                Restored.Remove(tool.Folder);
                return (restore, output);
            }

            output.Clear();
        }

        var code = 0;
        foreach (var command in Commands(tool, verb, files.Select(f => Path.GetRelativePath(tool.Folder, f).Replace('\\', '/')).ToList()))
        {
            var exit = FormatCommand.RunDotnet(tool.Folder, command, output.Add);
            code = code == 0 ? exit : code;
        }

        return (code, output);
    }

    /// <summary>
    /// --verify-no-changes: what CSharpier's check says about the C# files it formats under <paramref name="root"/> (or
    /// <paramref name="files"/> only), or null when they're as it formats them (or it doesn't format the repository).
    /// </summary>
    public static string? CheckLine(string root, IEnumerable<string>? files = null, Action<string>? log = null)
    {
        if (Find(root) is not { } tool)
        {
            return null;
        }

        // The whole repository: its folders, one options lookup each (Speckle: 612 files named one by one took 85 s, the
        // folder 1.7 s). Only C# files count: CSharpier 1.x checks XML files too (a nuget.config's line endings).
        var checkedFiles = files is null ? Folders(tool, root) : InScope(tool, root, files);
        if (checkedFiles.Count == 0)
        {
            return null;
        }

        log?.Invoke($"CSharpier: checking {(files is null ? "the C# files it formats" : $"{checkedFiles.Count} file{(checkedFiles.Count == 1 ? string.Empty : "s")}")}...");
        var (code, output) = Run(tool, "check", checkedFiles);
        if (FailedCSharp(code, output) is not { } failed)
        {
            return null;
        }

        return failed.Count == 0
            ? $"Not clean: CSharpier's check failed (exit code {code}): {string.Join(" ", output.Where(l => l.Trim().Length > 0).Take(3).Select(l => l.Trim()))}"
            : $"Not clean: CSharpier would change {failed.Count} file{(failed.Count == 1 ? string.Empty : "s")} ({string.Join(", ", failed.Take(5))}{(failed.Count > 5 ? $" and {failed.Count - 5} more" : string.Empty)}). "
                + "'stylebro-migrate format' runs it on the files it changes; for the others run 'dotnet csharpier format' on them.";
    }

    /// <summary>The folders the tool formats under <paramref name="root"/> (full paths).</summary>
    private static List<string> Folders(Tool tool, string root) =>
        tool.Scopes.Select(s => Path.GetFullPath(Path.Combine(root, s))).Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static IEnumerable<string> Above(string root)
    {
        for (var folder = new DirectoryInfo(Path.GetFullPath(root)); folder is not null; folder = folder.Parent)
        {
            yield return folder.FullName;
        }
    }

    private static int Major(string version) => int.TryParse(version.Split('.', '-')[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var major) ? major : 0;

    // The package's CSharpier.dll in the NuGet folder, for the newest framework it ships.
    private static string? PackageDll(string version)
    {
        var packages = Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } set ? set : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var tools = Path.Combine(packages, "csharpier.msbuild", version.ToLowerInvariant(), "tools", "csharpier");
        return Directory.Exists(tools)
            ? Directory.EnumerateDirectories(tools).Select(d => Path.Combine(d, "CSharpier.dll")).Where(File.Exists)
                .OrderByDescending(d => Version.TryParse(Path.GetFileName(Path.GetDirectoryName(d))!.TrimStart('n', 'e', 't', 'c', 'o', 'r', 'a', 'p'), out var v) ? v : new Version(0, 0))
                .FirstOrDefault()
            : null;
    }

    /// <summary>
    /// The repository's CSharpier: run 'dotnet' with <see cref="Command"/> in <see cref="Folder"/> (the tool manifest's, so dotnet
    /// finds it), 1.0's verbs or not, the folders it formats (relative to the root, "" for all) and the manifest (null: the package's copy).
    /// </summary>
    public sealed record Tool(string Folder, string[] Command, bool Verbs, IReadOnlyList<string> Scopes, string? Manifest);
}
