using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the twenty-seventh trial (PalCalc, System.IO.Abstractions, TheAlgorithms) found in init and migrate.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void RepositoryFolder_MovesToTheRoot_ForARootSolution_AnOutsideReference_OrNoProject()
    {
        // PalCalc: only PalCalc.sln at the root; init from PalCalc.UI/ (and from docres/, without C#) wrote its settings there.
        Write(".git/HEAD", "ref: refs/heads/main\n");
        Write("PalCalc.sln", "Project(\"{9A19103F}\") = \"App\", \"App\\App.csproj\", \"{1}\"\nEndProject\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("docs/readme.md", "x\n");
        string? workspace = null;

        var output = Capture(() => Assert.Equal(root, Program.RepositoryFolder(Path.Combine(root, "App"), ref workspace)));
        Assert.Contains("PalCalc.sln lists its projects", output, StringComparison.Ordinal);
        output = Capture(() => Assert.Equal(root, Program.RepositoryFolder(Path.Combine(root, "docs"), ref workspace)));
        Assert.Contains("it has no project of its own", output, StringComparison.Ordinal);

        // A project the solution doesn't list stays its own; one that references a project outside the folder doesn't.
        Assert.Equal(Path.Combine(root, "Lib"), Program.RepositoryFolder(Path.Combine(root, "Lib"), ref workspace));
        File.Delete(Path.Combine(root, "PalCalc.sln"));
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <ProjectReference Include=\"..\\Lib\\Lib.csproj\" />\n  </ItemGroup>\n</Project>\n");
        output = Capture(() => Assert.Equal(root, Program.RepositoryFolder(Path.Combine(root, "App"), ref workspace)));
        Assert.Contains("App/App.csproj references projects outside it", output, StringComparison.Ordinal);

        // A reference inside the folder doesn't count.
        Write("App/Sub/Sub.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <ProjectReference Include=\"..\\App.csproj\" />\n  </ItemGroup>\n</Project>\n");
        File.Delete(Path.Combine(root, "App", "App.csproj"));
        Assert.Equal(Path.Combine(root, "App"), Program.RepositoryFolder(Path.Combine(root, "App"), ref workspace));

        // Trial 22: a folder with its own 'root = true' (or its own props) stays put, whatever the root solution lists.
        Write("PalCalc.sln", "Project(\"{9A19103F}\") = \"App\", \"App\\Sub\\Sub.csproj\", \"{1}\"\nEndProject\n");
        Write("App/.editorconfig", "root = true\n");
        Assert.Equal(Path.Combine(root, "App"), Program.RepositoryFolder(Path.Combine(root, "App"), ref workspace));
    }

    [Fact]
    public void Conventions_CountUsingsRelativeToATypeOrTheRuntime()
    {
        // System.IO.Abstractions: 'using XFS = MockUnixSupport;' (a class in an enclosing namespace) and 'using Text;' inside
        // 'namespace System.IO.Abstractions...' counted as fully qualified, and BRO1126 rewrote 31 files.
        var trees = new[]
        {
            "namespace App { public static class Helper { } public delegate void D(); }",
            "namespace App.Tests;\n\nusing H = Helper;\nusing E = D;\nusing NUnit.Framework;\nusing System.Text;\nusing X = Other.Helper;\n\nclass C { }",
            "namespace System.IO.Abstractions.TestingHelpers { public static class MockUnixSupport { } }",
            "namespace System.IO.Abstractions.TestingHelpers.Tests\n{\n    using Text;\n    using Collections.Generic;\n    using XFS = MockUnixSupport;\n    using NUnit.Framework;\n    using Unknown.Stuff;\n\n    class T { }\n}",
        }.Select(t => Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(t)).ToList();
        var counts = Conventions.NewCounts();

        Conventions.CountRelativeUsings(trees, counts);

        // Relative: Helper, D, Text, Collections.Generic, MockUnixSupport; qualified: NUnit.Framework (twice), System.Text,
        // Other.Helper, Unknown.Stuff.
        Assert.Equal(new[] { 5, 5 }, counts[Conventions.RelativeUsingsKey]);
    }

    [Fact]
    public void Init_UnderTheMicrosoftStyle_SaysTheStaticsGetTheirPrefix_AndDropsLinesTheStyleRepeats()
    {
        // A plain init wrote '_camelCase' and BRO1307 off (2 statics without a prefix, 3 with 's_'); 'init --style microsoft'
        // kept both, and said "BRO1307 is off" while format gave the 2 their 's_'.
        var fields = string.Concat(Enumerable.Range(0, 12).Select(i => $"    private int _value{i};\n"));
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("App/C.cs", $"class C\n{{\n{fields}    private static int counter;\n    private static int gate;\n    private static int s_a;\n    private static int s_b;\n    private static int s_c;\n}}\n");
        var first = InitCommand.Detect(root, new Dictionary<string, string>(), []).Lines;
        Assert.Contains("stylebro_private_field_naming = _camelCase", first);
        Assert.Contains("dotnet_diagnostic.BRO1307.severity = none", first);

        var style = InitCommand.StyleSettings("microsoft")!;
        var (lines, report, _, _) = InitCommand.Detect(root, new Dictionary<string, string>(), [], first, style);
        Assert.DoesNotContain("stylebro_private_field_naming = _camelCase", lines);
        Assert.DoesNotContain("dotnet_diagnostic.BRO1307.severity = none", lines);
        Assert.Contains(report, l => l.Contains("private static fields: 2 without a prefix, 3 with one -> the style's s_camelCase applies: the 2 fields without one get 's_'", StringComparison.Ordinal));
        Assert.DoesNotContain(report, l => l.Contains("BRO1307", StringComparison.Ordinal));

        // A line the style says otherwise stays.
        Assert.False(InitCommand.SaysTheSame(new[] { "# init: x: y", "stylebro_private_field_naming = camelCase" }, style));
        Assert.True(InitCommand.SaysTheSame(new[] { "# init: x: y", "stylebro_private_field_naming = _camelCase" }, style));
        Assert.False(InitCommand.SaysTheSame(new[] { "stylebro_private_field_naming = _camelCase" }, null));
    }

    [Fact]
    public void Init_NamesBothStaticKeys_InTheSummary()
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.StaticPrefixKey] = [0, 24];
        counts["private static readonly fields' prefix"] = [0, 12];
        var report = Conventions.Decide(counts, new Dictionary<string, string>()).Report;
        Assert.Contains(report, l => l.Contains("Kept your style for 2 settings: stylebro_private_static_field_naming, stylebro_private_mutable_static_field_naming", StringComparison.Ordinal));
    }

    [Fact]
    public void Init_SkipsALockedCSharpFile()
    {
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("App/Migrations/Locked.cs", "class Locked { }\n");
        Write("App/C.cs", "class C { }\n");
        using (new FileStream(Path.Combine(root, "App", "Migrations", "Locked.cs"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        }

        Assert.True(File.Exists(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Migrate_FollowsANamingRuleForStatics_TheCodeDoesntBreak()
    {
        // TheAlgorithms: SA1309 on (no '_'), the .editorconfig's naming rule asks for '_' on private static fields, and the code
        // has none: the block pinned 'camelCase' against the rule.
        Write("App/App.csproj", StyleCopProject);
        Write("App/C.cs", "namespace App;\n\npublic class C\n{\n    private int value;\n}\n");
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_naming_rule.statics.severity = warning\ndotnet_naming_rule.statics.symbols = statics\ndotnet_naming_rule.statics.style = underscored\ndotnet_naming_symbols.statics.applicable_kinds = field\ndotnet_naming_symbols.statics.applicable_accessibilities = private\ndotnet_naming_symbols.statics.required_modifiers = static\ndotnet_naming_style.underscored.capitalization = camel_case\ndotnet_naming_style.underscored.required_prefix = _\n");
        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Contains("stylebro_private_mutable_static_field_naming = _camelCase", result.Lines);
        Assert.Contains("stylebro_private_field_naming = camelCase", result.Lines);

        // A StyleCop-clean static without the '_' says the rule was never enforced: SA1309's style stays.
        Write("App/D.cs", "namespace App;\n\npublic class D\n{\n    private static int count;\n}\n");
        Assert.Contains("stylebro_private_mutable_static_field_naming = camelCase", Migration.Generate(StyleCopSetup.Read(root), root).Lines);

        // IDE1006 off: no naming rule counts.
        File.Delete(Path.Combine(root, "App", "D.cs"));
        File.AppendAllText(Path.Combine(root, ".editorconfig"), "dotnet_diagnostic.IDE1006.severity = none\n");
        Assert.Contains("stylebro_private_mutable_static_field_naming = camelCase", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void VendoredFolders_IncludeAFolderWithAnotherCopyrightHoldersLicense()
    {
        // PalCalc: adonis-ui/ and DotNetKit.../ are forks with their own MIT license (other holders), 194 of 476 changed files.
        Write("LICENSE.txt", "Copyright 2024, Tyler Camp\n\nPermission is hereby granted\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("App/C.cs", "class C { }\n");
        Write("adonis-ui/LICENSE", "MIT License\n\nCopyright (c) 2018 Benjamin Rühl\n");
        Write("adonis-ui/src/AdonisUI/A.cs", "class A { }\n");
        Write("Packages/Core/LICENSE.md", "Copyright (c) 2023-2024 Tyler Camp and contributors\n");
        Write("Packages/Core/B.cs", "class B { }\n");
        Write("docs/LICENSE", "Copyright (c) Someone Else\n");

        Assert.Equal(new[] { "adonis-ui" }, Migration.VendoredFolders(root));
        Assert.DoesNotContain(Migration.OwnCSharpFiles(root), f => f.Contains("adonis-ui", StringComparison.Ordinal));

        // Without a root license there's nothing to compare.
        File.Delete(Path.Combine(root, "LICENSE.txt"));
        Assert.Empty(Migration.VendoredFolders(root));
    }
}
