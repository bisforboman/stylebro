using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-second trial (error-or, DbUp, figgle, EasyNetQ) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void ACommittedPackageNextToTheSolution_DoesNotHideTheProjects()
    {
        // DbUp commits src/dbup.1.2.3.nupkg next to src/DbUp.sln and src/Directory.Build.props (none at the root). src/ was
        // taken for an extracted package and skipped: init judged its conventions from no code and pinned the preset in a new
        // root Directory.Build.props nothing imports, and the preview added the package there, so format failed.
        Write("src/DbUp.sln", string.Empty);
        Write("src/Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>\n  </PropertyGroup>\n</Project>\n");
        Write("src/dbup.1.2.3.nupkg", "zip");
        Write("src/dbup-core/dbup-core.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/dbup-core/Own.cs", "namespace DbUp;\n\npublic class Own\n{\n}\n");
        Write("src/new.cs", "class New { }\n");

        Assert.Equal(new[] { Path.Combine(root, "src", "Directory.Build.props") }, Migration.PropsFiles(root));
        Assert.Contains(StyleCopSetup.EnumerateFiles(root), f => f.EndsWith("Own.cs", StringComparison.Ordinal));
        Assert.False(PreviewCommand.InPackage(root, "src/new.cs"));

        // An extracted package is still one (trial 31's packages folders).
        Write("packages/Old.Lib.1.0.0/Old.Lib.1.0.0.nupkg", "zip");
        Write("packages/Old.Lib.1.0.0/content/Helper.cs", "class Helper { }\n");
        Assert.DoesNotContain(StyleCopSetup.EnumerateFiles(root), f => f.EndsWith("Helper.cs", StringComparison.Ordinal));
        Assert.True(PreviewCommand.InPackage(root, "packages/Old.Lib.1.0.0/content/Helper.cs"));
    }

    [Fact]
    public void Migrate_AndInit_SayWhichFilesTheirEndOfLineConverts()
    {
        // error-or: '[*] end_of_line = crlf', 36 C# files CRLF and 12 LF as committed, nothing converting them. The migration
        // turned IDE0055 on and 'format' rewrote the 12 LF files to CRLF (12 whole-file diffs), and nothing said so.
        Write(".editorconfig", "root = true\n[*]\nend_of_line = crlf\n[*.cs]\ndotnet_diagnostic.SA1500.severity = warning\n");
        for (var i = 0; i < 12; i++)
        {
            var eol = i < 3 ? "\n" : "\r\n";
            Write($"A{i}.cs", $"class A{i}{eol}{{{eol}}}{eol}");
        }

        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        var note = "Line endings: your .editorconfig says end_of_line = crlf, and 3 of 12 C# files are LF as committed (git doesn't convert them): 'format' converts them to CRLF";
        Assert.Contains(result.Notes, n => n.StartsWith(note, StringComparison.Ordinal));
        Assert.DoesNotContain("end_of_line = lf", result.Lines);

        // init on the same files (without StyleCop's settings, which make init send it to migrate).
        Write(".editorconfig", "root = true\n[*]\nend_of_line = crlf\n");
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");
        Assert.Contains("  note     " + note, output, StringComparison.Ordinal);

        // IDE0055 off: 'format' converts nothing, so nothing to say.
        Write(".editorconfig", "root = true\n[*]\nend_of_line = crlf\n[*.cs]\ndotnet_diagnostic.SA1500.severity = warning\ndotnet_diagnostic.IDE0055.severity = none\n");
        Assert.DoesNotContain(Migration.Generate(StyleCopSetup.Read(root), root).Notes, n => n.StartsWith(note, StringComparison.Ordinal));

        // All LF: the files outvote the key, which says so itself (and 'format' converts nothing).
        for (var i = 0; i < 12; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        Write(".editorconfig", "root = true\n[*]\nend_of_line = crlf\n[*.cs]\ndotnet_diagnostic.SA1500.severity = warning\n");
        var notes = Migration.Generate(StyleCopSetup.Read(root), root).Notes;
        Assert.Contains(notes, n => n.Contains("says end_of_line = crlf, but 12 of 12 C# files are LF", StringComparison.Ordinal));
        Assert.DoesNotContain(notes, n => n.Contains("'format' converts them", StringComparison.Ordinal));
        Write(".editorconfig", "root = true\n[*]\nend_of_line = crlf\n");
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");
        Assert.Contains("says end_of_line = crlf, but 12 of 12 C# files are LF", output, StringComparison.Ordinal);
        Assert.DoesNotContain("'format' converts them", output, StringComparison.Ordinal);

        // All CRLF: nothing to convert.
        for (var i = 0; i < 12; i++)
        {
            Write($"A{i}.cs", $"class A{i}\r\n{{\r\n}}\r\n");
        }

        Assert.Null(Conventions.ConvertedMinorityNote(Conventions.EndingCounts(root), new Dictionary<string, string> { ["end_of_line"] = "crlf" }));
    }
}
