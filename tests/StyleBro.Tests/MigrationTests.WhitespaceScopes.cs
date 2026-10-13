using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>The migration judges the whitespace keys per file, like init (#171): a nearer .editorconfig decides them there.</summary>
public sealed partial class MigrationTests
{
    private const string NestedEditorConfig = "[*]\ninsert_final_newline = false\nend_of_line = crlf\n";

    [Fact]
    public void Migrate_ANestedEditorConfigTheFilesDontFollow_GetsTheDecision_AndASecondWriteChangesNothing()
    {
        // Bogus's Source/.editorconfig: the root block's lines never reach Source/, so 'format' would strip every last line
        // break and turn the endings CRLF.
        WriteStyleCopRepository(NestedEditorConfig);
        for (var i = 0; i < 10; i++)
        {
            Write($"Source/A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        var output = Capture(() => Assert.Equal(0, Program.Main(new[] { root, "--write" })));

        var nested = File.ReadAllText(Path.Combine(root, "Source", ".editorconfig"));
        var main = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.StartsWith(NestedEditorConfig + "\n" + InitCommand.WhitespaceBegin, nested, StringComparison.Ordinal);
        Assert.Contains("\nend_of_line = lf\n", nested, StringComparison.Ordinal);
        Assert.Contains("\ninsert_final_newline = true\n" + InitCommand.WhitespaceEnd, nested, StringComparison.Ordinal);
        Assert.Contains("Wrote the whitespace settings it decides to Source/.editorconfig.", output, StringComparison.Ordinal);
        Assert.Contains("Source/.editorconfig says insert_final_newline = false, but 10 of 10 C# files end with a line break", output, StringComparison.Ordinal);

        // The root's own block has nothing to say about files it doesn't reach.
        Assert.DoesNotContain("end_of_line = ", main.Substring(main.IndexOf(Migration.BeginMarker, StringComparison.Ordinal)), StringComparison.Ordinal);
        Assert.DoesNotContain("insert_final_newline = ", main.Substring(main.IndexOf(Migration.BeginMarker, StringComparison.Ordinal)), StringComparison.Ordinal);

        Capture(() => Assert.Equal(0, Program.Main(new[] { root, "--write" })));
        Assert.Equal(nested, File.ReadAllText(Path.Combine(root, "Source", ".editorconfig")));
        Assert.Equal(main, File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Migrate_ANestedEditorConfigTheFilesFollow_StaysAsItIs()
    {
        WriteStyleCopRepository(NestedEditorConfig);
        for (var i = 0; i < 10; i++)
        {
            Write($"Source/A{i}.cs", $"class A{i}\r\n{{\r\n}}");
        }

        Capture(() => Assert.Equal(0, Program.Main(new[] { root, "--write" })));

        Assert.Equal(NestedEditorConfig, File.ReadAllText(Path.Combine(root, "Source", ".editorconfig")));

        // Files the root's keys don't reach don't vote on them either (else these 10 would flip the root's to theirs).
        var main = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        main = main.Substring(main.IndexOf(Migration.BeginMarker, StringComparison.Ordinal));
        Assert.DoesNotContain("end_of_line = ", main, StringComparison.Ordinal);
        Assert.DoesNotContain("insert_final_newline = ", main, StringComparison.Ordinal);
    }

    [Fact]
    public void Migrate_AndInit_RemoveAWhitespaceBlockNothingNeedsAnyMore()
    {
        // Source/.editorconfig no longer sets the keys the block was written for.
        const string Own = "[*]\nindent_size = 4\n";
        var stale = Own + "\n" + InitCommand.WhitespaceBlock(["insert_final_newline = unset"]);
        WriteStyleCopRepository(stale);
        Write("Source/A.cs", "class A\n{\n}\n");

        var output = Capture(() => Assert.Equal(0, Program.Main(new[] { root, "--write" })));
        Assert.Equal(Own, File.ReadAllText(Path.Combine(root, "Source", ".editorconfig")));
        Assert.Contains("Removed the whitespace block from Source/.editorconfig", output, StringComparison.Ordinal);

        File.Delete(Path.Combine(root, ".editorconfig"));
        File.Delete(Path.Combine(root, "Source", "Lib.csproj"));
        Write("Source/.editorconfig", stale);
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(Own, File.ReadAllText(Path.Combine(root, "Source", ".editorconfig")));
    }

    /// <summary>A StyleCop repository: the package in Source/Lib.csproj, the root .editorconfig and <paramref name="nested"/> in Source/.</summary>
    private void WriteStyleCopRepository(string nested)
    {
        Write(".editorconfig", "root = true\n[*.cs]\ninsert_final_newline = true\nend_of_line = lf\ndotnet_diagnostic.SA1500.severity = warning\n");
        Write("Source/.editorconfig", nested);
        Write("Source/Lib.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
    }
}
