using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace StyleBro.Migrate;

/// <summary>
/// The StyleBro section init and '--write' put into the repository's AGENTS.md (owner's request 2026-10-10,
/// docs/decisions.md): what an AI agent editing the code should run, and that the findings the format leaves are kept on
/// purpose. Between markers like the .editorconfig block, so a second run replaces it. AGENTS.md is the cross-tool file;
/// a repository with only a CLAUDE.md gets an AGENTS.md too, and a hint to import it from CLAUDE.md.
/// </summary>
internal static class AgentsFile
{
    /// <summary>The file.</summary>
    public const string FileName = "AGENTS.md";

    /// <summary>The option that leaves AGENTS.md alone.</summary>
    public const string OptOut = "--no-agents-md";

    /// <summary>The start marker.</summary>
    public const string Begin = "<!-- BEGIN stylebro";

    /// <summary>The end marker.</summary>
    public const string End = "<!-- END stylebro -->";

    /// <summary>The section, markers included.</summary>
    public const string Section = Begin + " (written by stylebro-migrate; edits inside are replaced) -->\n"
        + "## StyleBro (C# style)\n"
        + "\n"
        + "This repository uses [StyleBro](https://bisforboman.github.io/stylebro/): analyzers whose fixes `dotnet format` applies.\n"
        + "\n"
        + "- After editing C# files, run `stylebro-migrate format --files <the .cs files you changed>` (seconds; the tool is the\n"
        + "  StyleBro.Migrate package: `dotnet tool install -g StyleBro.Migrate`).\n"
        + "- Before finishing, `stylebro-migrate format --verify-no-changes` must exit 0. It does when only findings kept on\n"
        + "  purpose are left.\n"
        + "- Don't hand-fix BRO warnings the format leaves: they are kept on purpose (a rename would break reflection, `nameof`,\n"
        + "  generated code, ...); the format output gives each one's reason. Leave them unless asked.\n"
        + "- Settings live in `.editorconfig` (the `stylebro-migrate` block); don't add `#pragma` or `[SuppressMessage]` for\n"
        + "  StyleBro rules unless asked.\n"
        + "- `--json` gives machine-readable output. Docs for agents: https://bisforboman.github.io/stylebro/agents/ (index:\n"
        + "  https://bisforboman.github.io/stylebro/llms.txt).\n"
        + End + "\n";

    /// <summary>The file with the section added, or replaced when it has one.</summary>
    public static string Apply(string? existing) => Migration.Apply(existing, Section, Begin, End);

    /// <summary>
    /// Writes the section into the root's AGENTS.md (<paramref name="write"/>), or says it would; nothing with
    /// <paramref name="optOut"/>. Returns the lines to print.
    /// </summary>
    public static List<string> Update(string root, bool write, bool optOut)
    {
        var lines = new List<string>();
        if (optOut)
        {
            return lines;
        }

        var path = Path.Combine(root, FileName);
        var existing = File.Exists(path) ? File.ReadAllText(path) : null;
        var text = Apply(existing);
        var changed = text != existing?.Replace("\r\n", "\n");
        JsonReport.Set("agentsMd", new JsonObject { ["file"] = FileName, ["created"] = existing is null, ["changed"] = changed, ["written"] = write && changed });
        if (!write)
        {
            lines.Add($"== {FileName} ({(existing is null ? "created" : "section added or updated")} with --write; {OptOut} leaves it alone)");
            lines.Add(Section.TrimEnd('\n'));
            return lines;
        }

        if (changed)
        {
            File.WriteAllText(path, text);
            lines.Add($"{(existing is null ? "Wrote" : "Updated")} the StyleBro section in {FileName} (instructions for AI agents; {OptOut} leaves it alone).");
        }

        if (existing is null && File.Exists(Path.Combine(root, "CLAUDE.md")))
        {
            lines.Add($"CLAUDE.md doesn't read {FileName} by itself: add a line '@{FileName}' to CLAUDE.md to include it.");
        }

        return lines;
    }
}
