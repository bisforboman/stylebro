using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
    /// The settings files to write instead of the root .editorconfig: <paramref name="Source"/> is the submodule's file the
    /// root one is a copy of (relative, '/'-separated), <paramref name="Folders"/> the top-level folders with projects.
    /// </summary>
    public sealed record SyncedConfig(string Source, IReadOnlyList<string> Folders);

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
}
