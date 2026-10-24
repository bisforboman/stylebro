using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-third trial (Sharprompt, Nerdbank.Streams, Autofac.Extensions.DependencyInjection, BaGet) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void ACommittedPackageNextToCode_IsNotAnExtractedPackage()
    {
        // BaGet commits tests/BaGet.Tests/TestData/TestData.1.2.3.nupkg next to C# code, with the project a folder above. The
        // folder was taken for an extracted package: 'format' fixed a file there that its snapshot never saw ("0 files changed").
        Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("TestData/TestData.1.2.3.nupkg", "zip");
        Write("TestData/Helper.cs", "class Helper { }\n");

        Assert.Contains(StyleCopSetup.EnumerateFiles(root), f => f.EndsWith("Helper.cs", StringComparison.Ordinal));
        Assert.Contains(FormatCommand.Snapshot(root).Keys, f => f.EndsWith("Helper.cs", StringComparison.Ordinal));
        Assert.False(PreviewCommand.InPackage(root, "TestData/Helper.cs"));

        // NuGet's own folders still are: packages.config's (side by side, and with -ExcludeVersion), a global packages folder's.
        Write("packages/Old.Lib.1.0.0/Old.Lib.1.0.0.nupkg", "zip");
        Write("packages/Old.Lib.1.0.0/content/A.cs", "class A { }\n");
        Write("packages/Other.Lib/Other.Lib.nupkg", "zip");
        Write("packages/Other.Lib/content/B.cs", "class B { }\n");
        Write("global/new.lib/2.0.0/new.lib.2.0.0.nupkg", "zip");
        Write("global/new.lib/2.0.0/content/C.cs", "class C { }\n");
        var files = StyleCopSetup.EnumerateFiles(root).Select(Path.GetFileName).ToList();
        Assert.DoesNotContain("A.cs", files);
        Assert.DoesNotContain("B.cs", files);
        Assert.DoesNotContain("C.cs", files);
        Assert.True(PreviewCommand.InPackage(root, "global/new.lib/2.0.0/content/C.cs"));

        // A folder named like its .nupkg with a project in it is a project's (a package packed next to it), not a package.
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("Lib/Lib.nupkg", "zip");
        Write("Lib/Code.cs", "class Code { }\n");
        Assert.Contains("Code.cs", StyleCopSetup.EnumerateFiles(root).Select(Path.GetFileName));
    }

    [Fact]
    public void Init_AndMigrate_SayWhichFilesTheirCharsetAndFinalNewlineChange()
    {
        // BaGet: '[*] charset = utf-8' with 16 of 248 C# files starting with a BOM. Init turned IDE0055 on and 'format' stripped
        // the 16 BOMs, and nothing said so (owner, 2026-10-24: "Warn only", as for end_of_line in trial 52).
        Write(".editorconfig", "root = true\n[*]\ncharset = utf-8\ninsert_final_newline = true\n");
        for (var i = 0; i < 12; i++)
        {
            File.WriteAllText(Path.Combine(root, $"A{i}.cs"), $"{(i < 2 ? "\uFEFF" : string.Empty)}class A{i}\n{{\n}}{(i == 5 ? string.Empty : "\n")}");
        }

        var charset = "Charset: your .editorconfig says charset = utf-8, and 2 of 12 C# files start with a BOM: 'format' removes the BOM from them.";
        var newline = "Final line breaks: your .editorconfig says insert_final_newline = true, and 1 of 12 C# files ends without a line break: 'format' adds a last line break to it.";
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");
        Assert.Contains("  note     " + charset, output, StringComparison.Ordinal);
        Assert.Contains("  note     " + newline, output, StringComparison.Ordinal);
        var own = new Dictionary<string, string> { ["charset"] = "utf-8" };
        Assert.Null(Conventions.ConvertedMinorityFileNote(new Dictionary<string, int[]> { ["charset"] = [12, 0] }, own, "charset"));

        // migrate, with StyleCop's settings; IDE0055 off: 'format' changes nothing, so nothing to say.
        Write(".editorconfig", "root = true\n[*]\ncharset = utf-8\ninsert_final_newline = true\n[*.cs]\ndotnet_diagnostic.SA1500.severity = warning\n");
        var notes = Migration.Generate(StyleCopSetup.Read(root), root).Notes;
        Assert.Contains(notes, n => n.StartsWith(charset, StringComparison.Ordinal));
        Assert.Contains(notes, n => n.StartsWith(newline, StringComparison.Ordinal));
        Write(".editorconfig", "root = true\n[*]\ncharset = utf-8\ninsert_final_newline = true\n[*.cs]\ndotnet_diagnostic.SA1500.severity = warning\ndotnet_diagnostic.IDE0055.severity = none\n");
        Assert.DoesNotContain(Migration.Generate(StyleCopSetup.Read(root), root).Notes, n => n.StartsWith("Charset: ", StringComparison.Ordinal));
    }
}
