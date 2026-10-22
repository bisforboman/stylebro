using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// A root .editorconfig the build puts back (trial 40, SixLabors' ImageSharp.Web: SixLaborsCopyConfigFiles copies
/// shared-infrastructure/.editorconfig, a git submodule's, over it before every build). A block written there is gone after
/// the first build, so format and check see other settings than the build. Owner's choice: the block goes into an
/// .editorconfig in each top-level folder with projects instead, which adds to the root's.
/// </summary>
internal static partial class Migration
{
    /// <summary>
    /// The redirection for <paramref name="root"/>, or null: its .editorconfig has the same text as an .editorconfig inside a
    /// git submodule, and every C# project lies in a top-level folder (one at the root would see only the root's file).
    /// </summary>
    public static SyncedConfig? Synced(string root)
    {
        var path = Path.Combine(root, ".editorconfig");
        if (!File.Exists(path))
        {
            return null;
        }

        var text = File.ReadAllText(path).Replace("\r\n", "\n");
        var source = StyleCopSetup.NestedRepositories(root)
            .SelectMany(submodule => Directory.EnumerateFiles(Path.Combine(root, submodule), ".editorconfig", SearchOption.AllDirectories))
            .FirstOrDefault(candidate => File.ReadAllText(candidate).Replace("\r\n", "\n") == text);
        if (source is null)
        {
            return null;
        }

        var projects = StyleCopSetup.EnumerateFiles(root)
            .Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .ToList();
        if (projects.Count == 0 || projects.Any(p => !p.Contains('/')))
        {
            return null;
        }

        var folders = projects.Select(p => p.Substring(0, p.IndexOf('/'))).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        return new SyncedConfig(Path.GetRelativePath(root, source).Replace('\\', '/'), folders);
    }

    /// <summary>What init and migrate say when they write <see cref="Synced"/>'s folder files.</summary>
    public static string SyncedLine(SyncedConfig synced) =>
        $"The root .editorconfig is a copy of {synced.Source} (a git submodule's), which the build puts back: the settings go into "
        + string.Join(", ", synced.Folders.Select(f => f + "/.editorconfig")) + " instead, which add to the root's.";

    /// <summary>
    /// The file holding the stylebro-migrate block of <paramref name="root"/>: the .editorconfig of <see cref="BlockFolder"/>,
    /// or the first of <see cref="Synced"/>'s folder files that has it (its paths are relative to the root all the same).
    /// </summary>
    public static string BlockFile(string root)
    {
        var path = Path.Combine(BlockFolder(root), ".editorconfig");
        if (File.Exists(path) && File.ReadAllText(path).Contains(BeginMarker, StringComparison.Ordinal))
        {
            return path;
        }

        return Synced(root)?.Folders.Select(f => Path.Combine(root, f, ".editorconfig"))
            .FirstOrDefault(f => File.Exists(f) && File.ReadAllText(f).Contains(BeginMarker, StringComparison.Ordinal)) ?? path;
    }

    /// <summary>
    /// <paramref name="block"/> (written for the root) for <paramref name="folder"/>'s .editorconfig: a section whose path
    /// starts with the folder loses that part, one for a path elsewhere goes (it matches nothing below the folder), and one
    /// without a folder ('*.cs', '**/x') stays as it is. Comment lines stay, root-relative paths included (<see cref="BlockFile"/>).
    /// </summary>
    public static string Rebase(string block, string folder)
    {
        var result = new StringBuilder();
        var keep = true;
        foreach (var line in block.Replace("\r\n", "\n").Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var pattern = RebasePattern(trimmed.Substring(1, trimmed.Length - 2), folder);
                keep = pattern is not null;
                if (keep)
                {
                    result.Append('[').Append(pattern).Append("]\n");
                }

                continue;
            }

            // The end marker always stays, whatever section it closes.
            if (keep || trimmed.StartsWith(EndMarker, StringComparison.Ordinal))
            {
                result.Append(line).Append('\n');
            }
        }

        return result.ToString().TrimEnd('\n') + "\n";
    }

    /// <summary>
    /// The restored package whose file the root .editorconfig of <paramref name="root"/> is a copy of (the stylebro-migrate
    /// block aside), or null. Trial 44, dailydevops/arguments: NetEvolve.Defaults' build copies its template.editorconfig over
    /// the root file before every build, so init's block was gone after the first build, which then reported 610 findings
    /// 'format' and 'check' didn't see. Its template .gitignore ignores nested .editorconfig files, so <see cref="Synced"/>'s
    /// folder files aren't a way out. Owner's decision 2026-10-22: say so, and the verdict is Not verified.
    /// </summary>
    public static PackageConfig? PackageCopy(string root)
    {
        var path = Path.Combine(root, ".editorconfig");
        if (!File.Exists(path))
        {
            return null;
        }

        var own = ConfigLines(Regex.Replace(File.ReadAllText(path), $"{BeginMarker}.*?{EndMarker}", string.Empty, RegexOptions.Singleline));
        if (own.Count == 0)
        {
            return null;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assets in AssetsFiles(root))
        {
            var text = File.ReadAllText(assets);
            if (!text.Contains("editorconfig", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var document = JsonDocument.Parse(text);
            if (!document.RootElement.TryGetProperty("libraries", out var libraries) || !document.RootElement.TryGetProperty("packageFolders", out var packageFolders))
            {
                continue;
            }

            var folders = packageFolders.EnumerateObject().Select(f => f.Name).ToList();
            foreach (var library in libraries.EnumerateObject())
            {
                if (!seen.Add(library.Name)
                    || !library.Value.TryGetProperty("type", out var type) || type.GetString() != "package"
                    || !library.Value.TryGetProperty("path", out var libraryPath) || !library.Value.TryGetProperty("files", out var files))
                {
                    continue;
                }

                var names = files.EnumerateArray().Select(f => f.GetString() ?? string.Empty).ToList();
                var folder = folders.Select(f => Path.Combine(f, libraryPath.GetString() ?? string.Empty)).FirstOrDefault(Directory.Exists);
                foreach (var file in names.Where(f => f.EndsWith("editorconfig", StringComparison.OrdinalIgnoreCase)))
                {
                    var candidate = folder is null ? null : Path.Combine(folder, file);
                    if (candidate is not null && File.Exists(candidate)
                        && (ConfigLines(File.ReadAllText(candidate)).SequenceEqual(own, StringComparer.Ordinal) || BuildFilesName(folder!, names, file)))
                    {
                        return new PackageConfig(library.Name.Replace('/', ' '), file);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Whether one of the package's build files (.props/.targets under build/, buildTransitive/, buildMultiTargeting/) names
    /// <paramref name="file"/> (its file name, or its path for a file named just '.editorconfig'). Trial 45, dailydevops/guard:
    /// the committed root file was an older version of NetEvolve.Defaults' template (the build rewrites it, nobody commits
    /// that), so comparing the text missed it. Owner's decision 2026-10-22: the targets naming the file is enough.
    /// </summary>
    private static bool BuildFilesName(string folder, IEnumerable<string> names, string file)
    {
        var name = Path.GetFileName(file);
        var needle = name.Equals(".editorconfig", StringComparison.OrdinalIgnoreCase) ? file : name;
        return names.Where(n => (n.StartsWith("build", StringComparison.OrdinalIgnoreCase) && n.Contains('/'))
                && (n.EndsWith(".props", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".targets", StringComparison.OrdinalIgnoreCase)))
            .Select(n => Path.Combine(folder, n))
            .Any(n => File.Exists(n) && File.ReadAllText(n).Replace('\\', '/').Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>What init and migrate say after writing into a root .editorconfig that <see cref="PackageCopy"/> finds.</summary>
    public static string PackageCopyLine(PackageConfig copy) =>
        $"Note: the root .editorconfig is a copy of {copy.Package}'s {copy.File}, and that package's build copies it back over the file before "
        + "every build: these settings are gone after the next build, which then reports what 'bro format' and 'bro check' don't. Turn "
        + "the package's copying off (or remove the package), then run this again; until then format and check say Not verified.";

    /// <summary>The gap format and check name for a <see cref="PackageCopy"/> (<see cref="FormatCommand.Gap"/>).</summary>
    public static string PackageCopyGap(PackageConfig copy) =>
        FormatCommand.Gap("the build's settings", $"the root .editorconfig is a copy of {copy.Package}'s {copy.File}, which the build copies back over it, so the build doesn't read the settings format checked with");

    /// <summary>A section's pattern for <paramref name="folder"/>'s .editorconfig, or null when it matches nothing there.</summary>
    private static string? RebasePattern(string pattern, string folder)
    {
        pattern = pattern.TrimStart('/');
        if (!pattern.Contains('/') || pattern.StartsWith("**/", StringComparison.Ordinal))
        {
            return pattern;
        }

        // '{src,tests}/**.cs': the folder's own alternative.
        if (pattern.StartsWith('{') && pattern.IndexOf("}/", StringComparison.Ordinal) is var close and > 0)
        {
            var alternatives = pattern.Substring(1, close - 1).Split(',');
            return alternatives.Contains(folder, StringComparer.Ordinal) ? pattern.Substring(close + 2) : null;
        }

        return pattern.StartsWith(folder + "/", StringComparison.Ordinal) ? pattern.Substring(folder.Length + 1) : null;
    }

    /// <summary>The non-empty lines of an .editorconfig, trimmed: a copy compares equal whatever its blank lines and line endings.</summary>
    private static List<string> ConfigLines(string text) =>
        text.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

    /// <summary>The restore outputs (project.assets.json) below <paramref name="root"/>: each project's obj/, and artifacts/obj or obj/ at the root (UseArtifactsOutput, a shared BaseIntermediateOutputPath).</summary>
    private static IEnumerable<string> AssetsFiles(string root)
    {
        var projects = StyleCopSetup.EnumerateFiles(root).Where(f => Path.GetExtension(f).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.Combine(Path.GetDirectoryName(p)!, "obj", "project.assets.json"));
        var shared = new[] { Path.Combine(root, "artifacts", "obj"), Path.Combine(root, "obj") }.Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "project.assets.json", SearchOption.AllDirectories));
        return projects.Concat(shared).Where(File.Exists).Distinct(StringComparer.Ordinal);
    }

    /// <summary>
    /// A package file the root .editorconfig is a copy of (<see cref="PackageCopy"/>): <paramref name="Package"/> is its id and
    /// version ('NetEvolve.Defaults 2.8.51'), <paramref name="File"/> its path inside the package.
    /// </summary>
    public sealed record PackageConfig(string Package, string File);

    /// <summary>
    /// The settings files to write instead of the root .editorconfig: <paramref name="Source"/> is the submodule's file the
    /// root one is a copy of (relative, '/'-separated), <paramref name="Folders"/> the top-level folders with projects.
    /// </summary>
    public sealed record SyncedConfig(string Source, IReadOnlyList<string> Folders);
}
