using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;

namespace StyleBro.Migrate;

/// <summary>
/// The StyleBro skill init and '--write' put into the repository (owner's request 2026-11-04,
/// docs/decisions/2026-11-04-a-stylebro-skill-for-ai-agents.md): the plugin marketplace's SKILL.md, embedded, at
/// <see cref="FileName"/>, where Claude Code, GitHub Copilot and Cursor look for a repository's skills. The whole file is
/// bro's: a second run replaces it, but only when it has <see cref="Marker"/>; a SKILL.md someone else wrote stays. Opt-in
/// with <see cref="Option"/>, like <see cref="AgentsFile"/>.
/// </summary>
internal static class SkillFile
{
    /// <summary>The file, relative to the repository root, with '/'.</summary>
    public const string FileName = ".claude/skills/stylebro/SKILL.md";

    /// <summary>The option that writes the skill.</summary>
    public const string Option = "--skill";

    /// <summary>The line after the front matter that marks the file as bro's.</summary>
    public const string Marker = "<!-- written by bro (StyleBro.Tool); bro init or migrate with --write --skill replaces this file -->";

    /// <summary>Gets the skill as the plugin serves it.</summary>
    public static string Source { get; } = Read();

    /// <summary>Gets the file bro writes: <see cref="Source"/> with <see cref="Marker"/> after the front matter.</summary>
    public static string Text { get; } = WithMarker(Source);

    /// <summary>
    /// Writes the skill into the root (<paramref name="write"/>), or says it would; only when <paramref name="requested"/>.
    /// Returns the lines to print.
    /// </summary>
    public static List<string> Update(string root, bool write, bool requested)
    {
        var lines = new List<string>();
        if (!requested)
        {
            return lines;
        }

        var path = Path.Combine(root, FileName.Replace('/', Path.DirectorySeparatorChar));
        var existing = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : null;
        var ours = existing is null || existing.Contains(Marker, StringComparison.Ordinal);
        var changed = ours && existing != Text;
        JsonReport.Set("skill", new JsonObject { ["file"] = FileName, ["created"] = existing is null, ["changed"] = changed, ["written"] = write && changed });
        if (!ours)
        {
            lines.Add($"Left {FileName} as it is: bro didn't write it. Delete it and run again to get StyleBro's skill.");
            return lines;
        }

        if (!write)
        {
            lines.Add(changed
                ? $"== {FileName} ({(existing is null ? "created" : "updated")} with --write): StyleBro's skill for AI agents, https://bisforboman.github.io/stylebro/agents/#the-skill"
                : $"== {FileName}: up to date.");
            return lines;
        }

        if (changed)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, Text);
            lines.Add($"{(existing is null ? "Wrote" : "Updated")} StyleBro's skill for AI agents: {FileName}.");
        }

        return lines;
    }

    /// <summary><paramref name="source"/> with <see cref="Marker"/> on the line after the front matter.</summary>
    internal static string WithMarker(string source)
    {
        source = source.Replace("\r\n", "\n");
        var end = source.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (!source.StartsWith("---\n", StringComparison.Ordinal) || end < 0)
        {
            throw new InvalidOperationException("SKILL.md has no front matter.");
        }

        end += "\n---\n".Length;
        return source[..end] + Marker + "\n" + source[end..];
    }

    private static string Read()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Skill.md")
            ?? throw new InvalidOperationException("Skill.md is missing from the tool.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n");
    }
}
