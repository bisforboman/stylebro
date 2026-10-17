using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the twenty-eighth trial (ZLogger, EpubReader, Jackett) found in init and migrate.</summary>
public sealed partial class MigrationTests
{
    private const string UnderscoredStaticsRule = "dotnet_naming_rule.statics.severity = warning\ndotnet_naming_rule.statics.symbols = statics\ndotnet_naming_rule.statics.style = underscored\ndotnet_naming_symbols.statics.applicable_kinds = field\ndotnet_naming_symbols.statics.applicable_accessibilities = private\ndotnet_naming_symbols.statics.required_modifiers = static\ndotnet_naming_style.underscored.capitalization = camel_case\ndotnet_naming_style.underscored.required_prefix = _\n";

    [Fact]
    public void Init_TakesEndOfLineUnset_ForNoSetting()
    {
        // ZLogger: '[*] end_of_line = lf' and '[*.cs] end_of_line = unset', every C# file LF: init wrote nothing, and the build's
        // IDE0055 reported the lines 'format' wrote with the OS's ending.
        Write(".editorconfig", "root = true\n\n[*]\nend_of_line = lf\n\n[*.cs]\nindent_size = 4\nend_of_line = unset\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"App/C{i}.cs", $"namespace App;\n\npublic class C{i}\n{{\n}}\n");
        }

        Assert.DoesNotContain("end_of_line", Migration.OwnSettings(root).Keys);
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));
        Assert.Contains("\nend_of_line = lf\n", output.Replace("\r\n", "\n"), StringComparison.Ordinal);

        // The team's lf stays the team's.
        Write(".editorconfig", "root = true\n\n[*.cs]\nend_of_line = lf\n");
        Assert.Equal("lf", Migration.OwnSettings(root)["end_of_line"]);
    }

    [Fact]
    public void Migrate_WithALockedFile_WritesNothing()
    {
        // #238 promised no crash on a locked file; the suppression rewrite still read and wrote directly, and crashed after one
        // pragma was rewritten.
        Write("App/App.csproj", StyleCopProject);
        Write("App/A.cs", "namespace App;\n\npublic class A\n{\n#pragma warning disable SA1309\n    private int _a;\n#pragma warning restore SA1309\n}\n");
        Write("App/B.cs", "namespace App;\n\npublic class B\n{\n#pragma warning disable SA1309\n    private int _b;\n#pragma warning restore SA1309\n}\n");
        var a = Path.Combine(root, "App", "A.cs");
        var before = File.ReadAllText(a);

        // Not even readable (locked), then readable but not writable (read-only): both stop the run before anything is written.
        var b = Path.Combine(root, "App", "B.cs");
        foreach (var locked in new[] { true, false })
        {
            string output;
            if (locked)
            {
                using (new FileStream(b, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    output = Capture(() => Assert.Equal(1, Program.Migrate(new[] { root, "--write" })));
                }
            }
            else
            {
                File.SetAttributes(b, FileAttributes.ReadOnly);
                output = Capture(() => Assert.Equal(1, Program.Migrate(new[] { root, "--write" })));
                File.SetAttributes(b, FileAttributes.Normal);
            }

            Assert.Contains("Couldn't read or write this file (open in another program?): App/B.cs. Nothing was written", output, StringComparison.Ordinal);
            Assert.Equal(before, File.ReadAllText(a));
            Assert.False(File.Exists(Path.Combine(root, ".editorconfig")));
        }

        // Unlocked, both files get the replacing id.
        Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write" })));
        Assert.Contains("BRO1303", File.ReadAllText(a), StringComparison.Ordinal);
    }

    [Fact]
    public void Init_SaysWhichFilesItCouldntRead()
    {
        // A locked Own.cs gave "1 place" instead of 11 without a word, and a locked vendored LICENSE turned 4 rules off.
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/Own.cs", "namespace App;\n\npublic class Own\n{\n}\n");
        Write("LICENSE", "MIT License\n\nCopyright (c) 2024 Acme Corporation\n");
        Write("App/MiniJson/LICENSE", "MIT License\n\nCopyright (c) 2013 Calvin Rien\n");
        Write("App/MiniJson/Json.cs", "namespace App.MiniJson;\n\npublic class Json\n{\n}\n");
        Assert.Equal(["App/MiniJson"], Migration.VendoredFolders(root));

        string output;
        using (new FileStream(Path.Combine(root, "App", "Own.cs"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        using (new FileStream(Path.Combine(root, "App", "MiniJson", "LICENSE"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));

            // The vendored folder's verdict is open: its code isn't counted, and it isn't marked.
            Assert.Equal(["App/MiniJson"], Migration.UndecidedLicenseFolders(root, StyleCopSetup.EnumerateFiles(root).ToList()));
            Assert.Empty(Migration.VendoredFolders(root));
        }

        Assert.Contains("Couldn't read 2 files (open in another program?), so the counts leave them out: App/MiniJson/LICENSE, App/Own.cs.", output, StringComparison.Ordinal);
        Assert.Contains("Without App/MiniJson/LICENSE there's no telling whether code with its own license file is another project's: that code is left out of the counts too", output, StringComparison.Ordinal);
        Assert.Null(Migration.UnreadableLine(Path.Combine(root, "Other")));

        // Without the root's license nothing can be compared either.
        using (new FileStream(Path.Combine(root, "LICENSE"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Equal(["App/MiniJson"], Migration.UndecidedLicenseFolders(root, StyleCopSetup.EnumerateFiles(root).ToList()));
        }

        // Read again, the files aren't reported any more.
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));
        Assert.Null(Migration.UnreadableLine(root));
        Assert.Empty(Migration.UndecidedLicenseFolders(root, StyleCopSetup.EnumerateFiles(root).ToList()));
    }

    [Theory]
    [InlineData("   APPENDIX: How to apply the Apache License to your work.\n\n      Copyright [yyyy] [name of copyright owner]\n", null)]
    [InlineData("MIT License\n\nCopyright (c) <year> <copyright holders>\n", null)]
    [InlineData("Copyright {yyyy} {name of copyright owner}\n", null)]
    [InlineData("      Object form, made available under the License, as indicated by a\n      copyright notice that is included in or attached to the work\n", null)]
    [InlineData("MIT License\n\nCopyright (c) 2024 Acme Corporation\n", "acme corporation")]
    [InlineData("Copyright [yyyy] [name of copyright owner]\nCopyright 2020 Jane Doe <jane@example.com>\n", "jane doe jane example com")]
    public void CopyrightHolder_IgnoresTemplatePlaceholdersAndProse(string license, string? holder)
    {
        Assert.Equal(holder, Migration.CopyrightHolder(license));
    }

    [Fact]
    public void VendoredFolders_NeedARealHolderOnBothSides()
    {
        // Trial 28: the root Apache license's appendix line 'Copyright [yyyy] [name of copyright owner]' was the holder, and
        // every folder with its own license (the team's own Extensions/ too) became vendored.
        const string Apache = "                                 Apache License\n\n   APPENDIX: How to apply the Apache License to your work.\n\n      Copyright [yyyy] [name of copyright owner]\n";
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/Own.cs", "namespace App;\n\npublic class Own\n{\n}\n");
        Write("LICENSE", Apache);
        Write("App/Extensions/LICENSE", "MIT License\n\nCopyright (c) 2025 Acme Corporation\n");
        Write("App/Extensions/Ext.cs", "namespace App;\n\npublic class Ext\n{\n}\n");
        Write("App/MiniJson/LICENSE", "MIT License\n\nCopyright (c) 2013 Calvin Rien\n");
        Write("App/MiniJson/MiniJson.cs", "namespace App;\n\npublic class MiniJson\n{\n}\n");
        Assert.Empty(Migration.VendoredFolders(root));

        // The reverse: the team's MIT license at the root, a sub-folder with Apache's template text.
        Write("LICENSE", "MIT License\n\nCopyright (c) 2024 Acme Corporation\n");
        Write("App/Extensions/LICENSE", Apache);
        Assert.Equal(["App/MiniJson"], Migration.VendoredFolders(root));
    }

    [Fact]
    public void AnEditorConfigShownInTheSolution_IsntCopied()
    {
        // EpubReader: '<None Include="..\.editorconfig"><Link>.editorconfig</Link></None>' only shows it in Solution Explorer.
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" /><None Include=\"..\\.editorconfig\"><Link>.editorconfig</Link></None></ItemGroup></Project>");
        Assert.Null(StyleCopSetup.Read(root).EditorConfigCopiedBy);

        Write("Directory.Build.targets", "<Project><Target Name=\"C\"><Copy SourceFiles=\"shared/.editorconfig\" DestinationFolder=\"$(MSBuildThisFileDirectory)\" /></Target></Project>");
        Assert.Equal("Directory.Build.targets", StyleCopSetup.Read(root).EditorConfigCopiedBy);
    }

    [Fact]
    public void Migrate_FollowsANamingRuleForStaticReadOnlyFields_TheCodeDoesntBreak()
    {
        // A rule requiring just 'static' covers 'private static readonly object _gate': PascalCase renamed it against the rule.
        Write("App/App.csproj", StyleCopProject);
        Write("App/C.cs", "namespace App;\n\npublic class C\n{\n    private static readonly object _gate = new object();\n}\n");
        Write(".editorconfig", "root = true\n\n[*.cs]\n" + UnderscoredStaticsRule);
        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("stylebro_private_static_field_naming", StringComparison.Ordinal));
        Assert.Contains(result.Notes, n => n.StartsWith("Private static readonly fields: your naming rule asks for '_camelCase'", StringComparison.Ordinal));

        // IDE1006 off: no naming rule counts.
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_diagnostic.IDE1006.severity = none\n" + UnderscoredStaticsRule);
        Assert.Contains("stylebro_private_static_field_naming = PascalCase", Migration.Generate(StyleCopSetup.Read(root), root).Lines);

        // One that breaks the rule says StyleCop's PascalCase was what the code followed.
        Write(".editorconfig", "root = true\n\n[*.cs]\n" + UnderscoredStaticsRule);
        Write("App/D.cs", "namespace App;\n\npublic class D\n{\n    private static readonly object Lock = new object();\n}\n");
        Assert.Contains("stylebro_private_static_field_naming = PascalCase", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void OwnCSharpFiles_BelowNoProject_CountOnlyWhenNothingCouldCompileThem()
    {
        Write("Unity/Assets/U.cs", "class U { }\n");
        Write("tools/T.cs", "class T { }\n");

        // No project at all (a folder of scripts): everything counts.
        Assert.Equal(2, Migration.OwnCSharpFiles(root).Count());

        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/C.cs", "class C { }\n");
        Assert.Equal(["C.cs"], Migration.OwnCSharpFiles(root).Select(Path.GetFileName));

        // A file the project links by its path counts (ZLogger's generator links one file), the others still don't; a
        // property in the link could point anywhere.
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><Compile Include=\"..\\tools\\T.cs\" Link=\"T.cs\" /></ItemGroup></Project>\n");
        Assert.Equal(["C.cs", "T.cs"], Migration.OwnCSharpFiles(root).Select(Path.GetFileName).Order());
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><Compile Include=\"$(Shared)T.cs\" /></ItemGroup></Project>\n");
        Assert.Equal(3, Migration.OwnCSharpFiles(root).Count());
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");

        // A shared project (.projitems) can list files anywhere.
        Write("Shared/Shared.projitems", "<Project />\n");
        Assert.Equal(3, Migration.OwnCSharpFiles(root).Count());
    }

    [Fact]
    public void Conventions_SkipMisplacedSummaries()
    {
        // Below attributes or inside a body, a '///' is BRO1602's '//' to be, and BRO1616 skips it: not counted.
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText(
                """
                class A
                {
                    [System.Obsolete]
                    /// <summary>Below the attribute.</summary>
                    public void M()
                    {
                        /// <summary>In a body.</summary>
                        var x = 1;
                    }

                    /// <summary>Documents N.</summary>
                    public void N() { }
                }
                """),
            counts);

        Assert.Equal([0, 1], counts["stylebro_summary_layout"]);
    }

    [Fact]
    public void Init_SaysHowManyFieldsFollowTheNamingRule_AndCountsOnlyCompiledFiles()
    {
        // Jackett: "223 places -> the naming rule ... decides". ZLogger: 9 files of a Unity folder no project compiles counted.
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_naming_rule.fields.severity = warning\ndotnet_naming_rule.fields.symbols = fields\ndotnet_naming_rule.fields.style = underscored\ndotnet_naming_symbols.fields.applicable_kinds = field\ndotnet_naming_symbols.fields.applicable_accessibilities = private\ndotnet_naming_style.underscored.capitalization = camel_case\ndotnet_naming_style.underscored.required_prefix = _\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/C.cs", "namespace App;\n\npublic class C\n{\n    private int _a;\n    private int _b;\n    private int c;\n}\n");
        Write("Unity/Assets/U.cs", "namespace Unity;\n\npublic class U\n{\n    private int u;\n    private int v;\n    private int w;\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));
        Assert.Contains("private fields: 3 places -> the naming rule dotnet_naming_rule.fields decides: 2 follow it", output, StringComparison.Ordinal);

        // A project linking files from elsewhere could compile them: then they count.
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><Compile Include=\"..\\Unity\\Assets\\*.cs\" /></ItemGroup></Project>\n");
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));
        Assert.Contains("private fields: 6 places -> the naming rule dotnet_naming_rule.fields decides: 2 follow it", output, StringComparison.Ordinal);

        // So could any Compile item of a Directory.Build file.
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("Directory.Build.props", "<Project><ItemGroup><Compile Include=\"..\\Unity\\Assets\\U.cs\" /></ItemGroup></Project>\n");
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));
        Assert.Contains("private fields: 6 places", output, StringComparison.Ordinal);
    }
}
