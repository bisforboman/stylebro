using System.Text.Json.Nodes;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>StyleBro's skill for AI agents (owner's request 2026-11-04): --skill and the plugin marketplace.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Skill_IsOptIn()
    {
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.False(Directory.Exists(Path.Combine(root, ".claude")));
        Assert.DoesNotContain("SKILL.md", output, StringComparison.Ordinal);
        Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write" })));
        Assert.False(Directory.Exists(Path.Combine(root, ".claude")));
    }

    [Fact]
    public void Skill_DryRunNamesIt_InitAndMigrationWriteIt_ASecondRunChangesNothing()
    {
        var path = Path.Combine(root, ".claude", "skills", "stylebro", "SKILL.md");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, SkillFile.Option })));
        Assert.Contains($"== {SkillFile.FileName} (created with --write)", output, StringComparison.Ordinal);
        Assert.False(File.Exists(path));

        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write", SkillFile.Option })));
        Assert.Equal(SkillFile.Text, File.ReadAllText(path));
        Assert.Contains($"Wrote StyleBro's skill for AI agents: {SkillFile.FileName}.", output, StringComparison.Ordinal);

        // An older version of bro's skill is replaced.
        File.WriteAllText(path, "---\nname: stylebro\n---\n" + SkillFile.Marker + "\nold\n");
        output = Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write", SkillFile.Option })));
        Assert.Equal(SkillFile.Text, File.ReadAllText(path));
        Assert.Contains("Updated StyleBro's skill", output, StringComparison.Ordinal);

        File.SetLastWriteTimeUtc(path, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write", SkillFile.Option })));
        Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), File.GetLastWriteTimeUtc(path));
        Assert.DoesNotContain("SKILL.md", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Skill_SomeoneElsesSkillMd_IsLeftAsItIs()
    {
        Write(".claude/skills/stylebro/SKILL.md", "---\nname: stylebro\ndescription: Ours.\n---\n\nOur notes.\n");
        JsonReport.Begin("init");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write", SkillFile.Option })));
        var json = JsonNode.Parse(JsonReport.End(0))!;

        Assert.Equal("---\nname: stylebro\ndescription: Ours.\n---\n\nOur notes.\n", File.ReadAllText(Path.Combine(root, ".claude", "skills", "stylebro", "SKILL.md")));
        Assert.Contains($"Left {SkillFile.FileName} as it is: bro didn't write it.", output, StringComparison.Ordinal);
        Assert.False(json["skill"]!["written"]!.GetValue<bool>());
        Assert.False(json["skill"]!["changed"]!.GetValue<bool>());
    }

    [Fact]
    public void Skill_Json_RecordsTheFile()
    {
        JsonReport.Begin("init");

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write", SkillFile.Option })));
        var json = JsonNode.Parse(JsonReport.End(0))!;

        Assert.Equal(SkillFile.FileName, json["skill"]!["file"]!.GetValue<string>());
        Assert.True(json["skill"]!["created"]!.GetValue<bool>());
        Assert.True(json["skill"]!["written"]!.GetValue<bool>());
    }

    [Fact]
    public void Skill_IsThePluginsSkillMd_WithTheMarkerAfterTheFrontMatter()
    {
        var plugin = File.ReadAllText(Path.Combine(RepositoryRoot(), "plugins", "stylebro", "skills", "stylebro", "SKILL.md")).Replace("\r\n", "\n");

        Assert.Equal(plugin, SkillFile.Source);
        Assert.StartsWith("---\nname: stylebro\ndescription: ", SkillFile.Text, StringComparison.Ordinal);
        Assert.Contains("\n---\n" + SkillFile.Marker + "\n\n# StyleBro\n", SkillFile.Text, StringComparison.Ordinal);
        Assert.Equal(SkillFile.Source, SkillFile.Text.Replace(SkillFile.Marker + "\n", string.Empty, StringComparison.Ordinal));
        Assert.Throws<InvalidOperationException>(() => SkillFile.WithMarker("# No front matter\n"));

        // The Agent Skills format: a description of at most 1024 characters, on one line.
        var description = SkillFile.Source.Split('\n').Single(l => l.StartsWith("description: ", StringComparison.Ordinal));
        Assert.InRange(description.Length - "description: ".Length, 1, 1024);
    }

    [Fact]
    public void Skill_ThePluginMarketplaceServesIt()
    {
        var repository = RepositoryRoot();
        var marketplace = JsonNode.Parse(File.ReadAllText(Path.Combine(repository, ".claude-plugin", "marketplace.json")))!;
        var entry = marketplace["plugins"]!.AsArray().Single()!;
        var plugin = JsonNode.Parse(File.ReadAllText(Path.Combine(repository, "plugins", "stylebro", ".claude-plugin", "plugin.json")))!;

        Assert.Equal("stylebro", entry["name"]!.GetValue<string>());
        Assert.Equal("./plugins/stylebro", entry["source"]!.GetValue<string>());
        Assert.Equal("stylebro", plugin["name"]!.GetValue<string>());
        Assert.Equal(entry["description"]!.GetValue<string>(), plugin["description"]!.GetValue<string>());
    }

    [Fact]
    public void Skill_TheDocsNameTheOptionAndTheFile()
    {
        var page = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "agents.md")).Replace("\r\n", "\n");

        Assert.Contains(SkillFile.Option, page, StringComparison.Ordinal);
        Assert.Contains(SkillFile.FileName, page, StringComparison.Ordinal);
        Assert.Contains(SkillFile.Marker, page, StringComparison.Ordinal);
        Assert.Contains("/plugin marketplace add bisforboman/stylebro", page, StringComparison.Ordinal);
    }
}
