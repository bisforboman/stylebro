using System.Text.Json.Nodes;
using StyleBro.Analyzers;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>For scripts and AI agents (owner's request 2026-10-10): --json, format --files and the AGENTS.md section.</summary>
public sealed partial class MigrationTests
{
    private static readonly string[] SchemaFields =
    {
        "schemaVersion", "command", "previewOf", "path", "write", "settings", "conventions", "sonar", "agentsMd", "runs",
        "changesPerRule", "filesPerRule", "keptFindings", "patch", "clean", "exitCode",
    };

    [Fact]
    public void Json_Main_PrintsOnlyTheReportOnStdout_AndTheTextOnStderr()
    {
        Write("A.cs", "class A { private int _a, _b, _c; }\n");
        string? stdout = null;

        var stderr = CaptureError(() => stdout = Capture(() => Assert.Equal(0, Program.Main(new[] { "init", root, "--json" }))));

        var json = JsonNode.Parse(stdout!)!.AsObject();
        Assert.Equal(SchemaFields.Order(StringComparer.Ordinal), json.Select(p => p.Key).Order(StringComparer.Ordinal));
        Assert.Equal(JsonReport.SchemaVersion, json["schemaVersion"]!.GetValue<int>());
        Assert.Equal("init", json["command"]!.GetValue<string>());
        Assert.False(json["write"]!.GetValue<bool>());
        Assert.Equal(0, json["exitCode"]!.GetValue<int>());
        Assert.Null(json["clean"]);
        Assert.Contains("== .editorconfig", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_Init_ListsTheSettingsWithWhy_TheConventions_AndAgentsMd()
    {
        Write("A.cs", "class A { private int _a, _b, _c; }\n");
        JsonReport.Begin("init");

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var json = JsonNode.Parse(JsonReport.End(0))!;

        var ide0055 = json["settings"]!.AsArray().Single(s => s!["key"]!.GetValue<string>() == "dotnet_diagnostic.IDE0055.severity")!;
        Assert.Equal((".editorconfig", "*.cs", "warning"), (ide0055["file"]!.GetValue<string>(), ide0055["section"]!.GetValue<string>(), ide0055["value"]!.GetValue<string>()));
        Assert.StartsWith("Formatting:", ide0055["why"]!.GetValue<string>(), StringComparison.Ordinal);
        var fields = json["conventions"]!.AsArray().Single(c => c!["key"]!.GetValue<string>() == "stylebro_private_field_naming")!;
        Assert.Equal(("kept", 3, 0), (fields["verdict"]!.GetValue<string>(), fields["counts"]!["_camelCase"]!.GetValue<int>(), fields["counts"]!["camelCase"]!.GetValue<int>()));
        Assert.True(json["write"]!.GetValue<bool>());
        Assert.True(json["agentsMd"]!["written"]!.GetValue<bool>());
    }

    [Fact]
    public void Json_Format_RecordsEachRun_ChangesPerRule_AndKeptFindings()
    {
        Write("C.cs", "class C { }\n");
        JsonReport.Begin("format");
        var run = 0;

        var code = FormatCommand.Run(new[] { root }, _ => { }, (args, _) =>
        {
            var report = args[Array.IndexOf(args, "--report") + 1];
            Directory.CreateDirectory(report);
            if (++run == 1)
            {
                // Another length: the snapshot compares length and write time, and the time can be the same tick.
                File.WriteAllText(Path.Combine(root, "C.cs"), "internal class C\n{\n}\n");
                File.WriteAllText(Path.Combine(report, "format-report.json"), "[{\"FilePath\":\"C.cs\",\"FileChanges\":[{\"LineNumber\":1,\"CharNumber\":9,\"DiagnosticId\":\"BRO1509\"},{\"LineNumber\":1,\"CharNumber\":1,\"DiagnosticId\":\"WHITESPACE\"}]}]");
            }
            else
            {
                File.AppendAllText(Environment.GetEnvironmentVariable(KeptFinding.Variable)!, new KeptFinding(Path.Combine(root, "C.cs"), 1, 7, "BRO1309", KeptReason.NameInString, "Rename 'c' to 'C'") + "\n");
            }

            return 0;
        });
        var json = JsonNode.Parse(JsonReport.End(code))!;

        Assert.Equal(0, code);
        Assert.True(json["clean"]!.GetValue<bool>());
        Assert.Equal(2, json["runs"]!.AsArray().Count);
        Assert.Equal("C.cs", json["runs"]![0]!["filesChanged"]![0]!.GetValue<string>());
        Assert.Empty(json["runs"]![1]!["filesChanged"]!.AsArray());
        Assert.Equal((1, 1), (json["changesPerRule"]!["BRO1509"]!.GetValue<int>(), json["changesPerRule"]!["IDE0055"]!.GetValue<int>()));
        var kept = json["keptFindings"]![0]!;
        Assert.Equal(("C.cs", 1, 7, "BRO1309", "NameInString"), (kept["path"]!.GetValue<string>(), kept["line"]!.GetValue<int>(), kept["column"]!.GetValue<int>(), kept["rule"]!.GetValue<string>(), kept["reason"]!.GetValue<string>()));
    }

    [Fact]
    public void Files_AreFormattedInTheirNearestProject_OnlyThoseFiles()
    {
        Write("src/A/A.csproj", "<Project />");
        Write("src/A/Sub/X.cs", "class X { }\n");
        Write("src/A/Y.cs", "class Y { }\n");
        Write("src/B/B.csproj", "<Project />");
        Write("src/B/Z.cs", "class Z { }\n");
        var calls = new List<string[]>();

        var code = FormatCommand.Run(
            new[] { "--files", Path.Combine(root, "src/A/Sub/X.cs"), Path.Combine(root, "src/B/Z.cs"), Path.Combine(root, "src/A/Y.cs"), "--severity", "warn" },
            _ => { },
            (args, _) =>
            {
                calls.Add(args);
                return 0;
            });

        Assert.Equal(0, code);
        Assert.Equal(
            new[]
            {
                $"{Path.Combine(root, "src", "A", "A.csproj")} --severity warn --include Sub/X.cs Y.cs",
                $"{Path.Combine(root, "src", "B", "B.csproj")} --severity warn --include Z.cs",
            },
            calls.Select(c => string.Join(" ", c)));
    }

    [Fact]
    public void Files_NotFound_OrOutsideTheNamedWorkspace_AreErrors()
    {
        Write("src/A/A.csproj", "<Project />");
        Write("other/X.cs", "class X { }\n");
        var errors = new List<string>();

        Assert.Null(FormatCommand.GroupFiles(new[] { "missing.cs" }, null, root, errors.Add));
        Assert.Null(FormatCommand.GroupFiles(new[] { "other/X.cs" }, Path.Combine(root, "src/A/A.csproj"), root, errors.Add));
        Assert.Null(FormatCommand.GroupFiles(new[] { "other/X.cs" }, null, root, errors.Add));
        Assert.Equal(new[] { "--files: not found: missing.cs", $"--files: other/X.cs isn't under {Path.Combine(root, "src", "A")}.", "--files: no project file above other/X.cs." }, errors);
    }

    [Fact]
    public void AgentsMd_IsCreated_UpdatedInPlace_AndLeftAloneWhenUnchanged()
    {
        var path = Path.Combine(root, AgentsFile.FileName);

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(AgentsFile.Section, File.ReadAllText(path));

        // Someone's own text around an old version of the section: the section is replaced, the rest stays.
        File.WriteAllText(path, "# Agents\n\nBuild with make.\n\n" + AgentsFile.Begin + " old -->\nold text\n" + AgentsFile.End + "\n\nMore.\n");
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var updated = File.ReadAllText(path);
        Assert.Equal("# Agents\n\nBuild with make.\n\n" + AgentsFile.Section + "\nMore.\n", updated);

        var written = File.GetLastWriteTimeUtc(path);
        File.SetLastWriteTimeUtc(path, written.AddHours(-1));
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(updated, File.ReadAllText(path));
        Assert.Equal(written.AddHours(-1), File.GetLastWriteTimeUtc(path));
        Assert.DoesNotContain(AgentsFile.FileName, output, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentsMd_OptOut_DryRun_AndTheMigration()
    {
        var path = Path.Combine(root, AgentsFile.FileName);

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write", AgentsFile.OptOut })));
        Assert.False(File.Exists(path));

        Assert.Contains("== AGENTS.md", Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))), StringComparison.Ordinal);
        Assert.False(File.Exists(path));

        Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write" })));
        Assert.Equal(AgentsFile.Section, File.ReadAllText(path));
    }

    [Fact]
    public void AgentsMd_TheDocsShowTheSectionAsWritten()
    {
        var page = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "agents.md")).Replace("\r\n", "\n");

        Assert.Contains(AgentsFile.Section, page, StringComparison.Ordinal);
        Assert.All(SchemaFields, field => Assert.Contains($"| `{field}` |", page, StringComparison.Ordinal));
        Assert.All(Enum.GetNames<KeptReason>(), reason => Assert.Contains($"`{reason}`", page, StringComparison.Ordinal));
    }

    [Fact]
    public void AgentsMd_NextToAClaudeMd_SaysHowToImportIt()
    {
        Write("CLAUDE.md", "Use tabs.\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.True(File.Exists(Path.Combine(root, AgentsFile.FileName)));
        Assert.Equal("Use tabs.\n", File.ReadAllText(Path.Combine(root, "CLAUDE.md")));
        Assert.Contains("add a line '@AGENTS.md' to CLAUDE.md", output, StringComparison.Ordinal);
    }
}
