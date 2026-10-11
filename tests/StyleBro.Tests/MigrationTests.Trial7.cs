using System.Text;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Findings of the seventh trial (Soulseek.NET, Stateless, realworlddotnet, spreadcheetah).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Migrate_WritesTheFilesLineEnding_UnlessTheRepositorySetsIt()
    {
        // Soulseek.NET (LF, no .gitattributes): without end_of_line a Windows 'format' run left 8 files with mixed endings.
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Contains("end_of_line = lf", result.Lines);
        Assert.Contains("Line endings: C# files 3 of 3 LF -> end_of_line = lf", result.Notes);

        Write(".editorconfig", "root = true\n[*]\nend_of_line = crlf\n");
        Assert.DoesNotContain(Migration.Generate(StyleCopSetup.Read(root), root).Lines, l => l.StartsWith("end_of_line", StringComparison.Ordinal));
    }

    [Fact]
    public void Migrate_APlainHeaderWithASemicolon_LeavesIde0073Off()
    {
        // file_header_template has no escapes: the SDK would cut the text at the ';' (checked with the .NET 10 SDK).
        Write("stylecop.json", """{ "settings": { "documentationRules": { "xmlHeader": false, "companyName": "C", "copyrightText": "No WARRANTY; see LICENSE." } } }""");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("dotnet_diagnostic.IDE0073.severity = none", result.Lines);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("file_header_template", StringComparison.Ordinal));
        Assert.Contains("';' or '#'", result.Reasons["SA1633"], StringComparison.Ordinal);

        Write("stylecop.json", """{ "settings": { "documentationRules": { "xmlHeader": false, "companyName": "C", "copyrightText": "Line 1.\nLine 2." } } }""");
        Assert.Contains(@"file_header_template = Line 1.\nLine 2.", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void Init_RunAgain_KeepsTheConventionsItWrote()
    {
        // Stateless: 28 of 29 switch sections without a blank line gave 'omit'. A second init after a format run that added
        // blank lines must not judge StyleBro's own output: the line stays.
        var omitted = string.Concat(Enumerable.Range(0, 4).Select(i => $"            case {i}: return {i};\n"));
        Write("A.cs", $"class A\n{{\n    int M(int x)\n    {{\n        switch (x)\n        {{\n{omitted}            default: return 0;\n        }}\n    }}\n}}\n");
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("stylebro_blank_line_between_switch_sections = omit", first, StringComparison.Ordinal);

        var spaced = string.Concat(Enumerable.Range(0, 4).Select(i => $"            case {i}: return {i};\n\n"));
        Write("A.cs", $"class A\n{{\n    int M(int x)\n    {{\n        switch (x)\n        {{\n{spaced}            default: return 0;\n        }}\n    }}\n}}\n");
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains("  set      switch sections: 4 of 4 after a blank line -> kept from the last init (omit)", output, StringComparison.Ordinal);

        // Deleting the line has it counted again.
        File.WriteAllText(Path.Combine(root, ".editorconfig"), first.Replace("stylebro_blank_line_between_switch_sections = omit\n", string.Empty));
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.DoesNotContain("stylebro_blank_line_between_switch_sections = omit", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_NoRestore_WithoutTheRestoreOutput_Stops()
    {
        // obj deleted: 'dotnet format --no-restore' loads no project, so the preview showed whitespace only.
        Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleBro.Analyzers\" Version=\"1.0.0\" /></ItemGroup></Project>\n");
        Write("C.cs", "class C { }\n");
        PreviewCommand.MakeRepository(root);
        var error = string.Empty;

        var output = Capture(() => error = CaptureError(() => Assert.Equal(1, PreviewCommand.Run("format", new[] { root, "--diff=" + Path.Combine(root, "out.patch"), "--no-restore" }, (_, _) => throw new InvalidOperationException("format ran")))));

        Assert.Contains(PreviewCommand.NoRestoreOutput, error, StringComparison.Ordinal);
        var kept = System.Text.RegularExpressions.Regex.Match(output, @"The copy is kept: (.*) \(log").Groups[1].Value;
        DeleteFolder(Path.GetDirectoryName(kept)!);
    }

    [Fact]
    public void Preview_PackageEdits_KeepTheByteOrderMark_AndSortPackageVersions()
    {
        var path = Path.Combine(root, "App.csproj");
        File.WriteAllText(path, "<Project>\n</Project>\n", new UTF8Encoding(true));
        PreviewCommand.EditFile(path, text => PreviewCommand.AddItem(text, "<X />"));
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, File.ReadAllBytes(path).Take(3));

        File.WriteAllText(path, "<Project />", new UTF8Encoding(false));
        PreviewCommand.EditFile(path, text => text + "\n");
        Assert.Equal((byte)'<', File.ReadAllBytes(path)[0]);

        var packages = "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"Polly\" Version=\"8\" />\n    <PackageVersion Include=\"xunit\" Version=\"2\">\n    </PackageVersion>\n  </ItemGroup>\n</Project>\n";
        Assert.Equal(
            "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"Polly\" Version=\"8\" />\n    <PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />\n    <PackageVersion Include=\"xunit\" Version=\"2\">\n    </PackageVersion>\n  </ItemGroup>\n</Project>\n",
            PreviewCommand.AddPackageVersion(packages, "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />"));
        Assert.Equal(
            "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"Polly\" Version=\"8\" />\n    <PackageVersion Include=\"Zeta\" Version=\"1\" />\n  </ItemGroup>\n</Project>\n",
            PreviewCommand.AddPackageVersion("<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"Polly\" Version=\"8\" />\n  </ItemGroup>\n</Project>\n", "<PackageVersion Include=\"Zeta\" Version=\"1\" />"));
    }

    [Fact]
    public void PackageHints_NameCentralPackageManagementOnlyWhenTheRepositoryUsesIt()
    {
        // realworlddotnet's file is 'Directory.build.props': named as on disk (MSBuild finds it on Windows only).
        var props = OperatingSystem.IsWindows() ? "Directory.build.props" : "Directory.Build.props";
        Write(props, "<Project><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" /></ItemGroup></Project>");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var plain = Program.PackageHints(StyleCopSetup.Read(root), root);
        Assert.Equal($"StyleCop.Analyzers is referenced in {props}: StyleBro.Analyzers goes there.", Assert.Single(plain));
        Assert.Equal(Path.Combine(root, props), Assert.Single(Migration.PropsFiles(root)));

        Write("Directory.Packages.props", "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>");
        Assert.EndsWith("goes there: its version in Directory.Packages.props (central package management).", Assert.Single(Program.PackageHints(StyleCopSetup.Read(root), root)), StringComparison.Ordinal);
    }

    [Fact]
    public void Migrate_TheNextStep_ComesAfterWhereThePackageGoes()
    {
        Write("Directory.Build.props", "<Project><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" /></ItemGroup></Project>");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var output = Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write" })));

        Assert.True(output.IndexOf("StyleCop.Analyzers is referenced in", StringComparison.Ordinal) < output.IndexOf(Program.NextStep, StringComparison.Ordinal));
    }
}
