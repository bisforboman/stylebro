using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>
/// StyleSharp.Analyzers (SST ids, a StyleCop successor) read like StyleCop (owner's decision 2026-10-14). Splat: SST1309 = error
/// ('_camelCase'), a leftover stylecop.json read with StyleCop's defaults because Sonar was there: 72 files lost their '_',
/// 'this.' was added, and the build had 973 errors.
/// </summary>
public sealed partial class MigrationTests
{
    private const string StyleSharpProject = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleSharp.Analyzers\" Version=\"3.40.1\" PrivateAssets=\"all\" /></ItemGroup></Project>\n";

    [Fact]
    public void StyleSharp_IsReadLikeStyleCop_AndItsStyleCopJsonLeftoverIsNot()
    {
        Write("Repro.csproj", StyleSharpProject);
        Write("stylecop.json", "{ \"settings\": { \"documentationRules\": { \"companyName\": \"X\" }, \"namingRules\": { \"tupleElementNameCasing\": \"camelCase\" } } }\n");
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.SST1306.severity = none\ndotnet_diagnostic.SST1309.severity = error\ndotnet_diagnostic.SST1311.severity = none\n");
        Write("Counter.cs", "namespace Repro;\n\npublic class Counter\n{\n    private int _count;\n    private int _step = 1;\n\n    public int Next() => _count += _step;\n}\n");

        var setup = StyleCopSetup.Read(root, _ => true);

        Assert.Equal("3.40.1", setup.StyleSharp?.Version);
        Assert.False(setup.StyleSharp!.WithStyleCop);
        Assert.False(setup.NotInstalled);
        Assert.Equal(Severity.Error, setup.Severities["SX1309"]); // SST1309 is StyleCop's SX1309, not SA1309
        Assert.False(setup.IsOn("SA1309"));
        Assert.False(setup.IsOn("SA1311"));
        Assert.True(setup.IsOn("SX1101")); // StyleSharp omits 'this.' by default (SST1117)
        Assert.False(setup.IsOn("SA1101"));
        Assert.False(setup.IsOn("SA1116")); // no SST rule stands for it (SST1116 is another rule)
        Assert.True(setup.IsOn("SA1117")); // SST1150-SST1171, all on
        Assert.False(setup.IsOn("SA1633")); // SST1633 checks nothing without file_header_template
        Assert.DoesNotContain(setup.Sources, s => s.EndsWith("stylecop.json", StringComparison.Ordinal));
        Assert.Contains(setup.Warnings, w => w.StartsWith("stylecop.json: StyleSharp doesn't read stylecop.json", StringComparison.Ordinal));

        var result = Migration.Generate(setup, root);
        Assert.Contains("stylebro_private_field_naming = _camelCase", result.Lines);
        Assert.Contains("dotnet_diagnostic.BRO1303.severity = error", result.Lines);
        Assert.Contains("dotnet_diagnostic.BRO1306.severity = none", result.Lines); // SST1311 off: static readonly fields aren't renamed
        Assert.Contains("dotnet_diagnostic.IDE0003.severity = warning", result.Lines);
        Assert.Contains("dotnet_style_qualification_for_field = false", result.Lines);
        Assert.Contains("dotnet_diagnostic.BRO1615.severity = none", result.Lines); // no XML header
        Assert.Contains("dotnet_diagnostic.IDE0073.severity = none", result.Lines);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith(StyleBro.Analyzers.Naming.TupleElementNames.CasingKey, StringComparison.Ordinal)); // PascalCase, StyleSharp's default
        Assert.Contains("csharp_using_directive_placement = outside_namespace", result.Lines); // SST1200
        Assert.Contains("SX1309", result.Covered);
        Assert.Contains(result.Notes, n => n.StartsWith("StyleSharp's SST1309", StringComparison.Ordinal));

        // The block records StyleSharp: after the package swap the setup is still read as StyleSharp's.
        var plan = Migration.Plan(setup, root, result);
        Write(".editorconfig", Migration.Apply(File.ReadAllText(Path.Combine(root, ".editorconfig")), Migration.Render(plan[".editorconfig"])));
        Write("Repro.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Assert.Equal("3.40.1", Migration.RecordedVersion(root, Migration.StyleSharpVersionPrefix));
        var swapped = StyleCopSetup.Read(root, _ => false);
        Assert.False(swapped.NotInstalled);
        Assert.Equal(Severity.Error, swapped.Severities["SX1309"]);
        Assert.Contains("stylebro_private_field_naming = _camelCase", Migration.Generate(swapped, root).Lines);
    }

    [Fact]
    public void StyleSharp_Options_AreStyleCopJsonsSettings()
    {
        Write("Directory.Packages.props", "<Project><ItemGroup><PackageVersion Include=\"StyleSharp.Analyzers\" Version=\"3.40.1\" /></ItemGroup></Project>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleSharp.Analyzers\" /></ItemGroup></Project>\n");
        Write(".editorconfig", "root = true\n[*.cs]\nstylesharp.instance_member_qualification = require_this\nstylesharp_tuple_element_naming = camel_case\n"
            + "stylesharp.SST1305.allowed_hungarian_prefixes = rx, db\nstylesharp.document_internal_elements = false\nfile_header_template = Copyright (c) Me.\\nAll rights reserved.\n"
            + "dotnet_diagnostic.SST1305.severity = warning\ndotnet_diagnostic.SST1160.severity = none\n");

        var setup = StyleCopSetup.Read(root, _ => true);

        Assert.True(setup.IsOn("SA1101"));
        Assert.False(setup.IsOn("SX1101"));
        Assert.False(setup.IsOn("SA1117")); // one of its list kinds is off
        Assert.True(setup.IsOn("SA1633"));
        Assert.True(setup.IsOn("SA1305"));
        var result = Migration.Generate(setup, root);
        Assert.Contains("dotnet_diagnostic.IDE0009.severity = warning", result.Lines);
        Assert.Contains("dotnet_style_qualification_for_field = true", result.Lines);
        Assert.Contains($"{StyleBro.Analyzers.Naming.TupleElementNames.CasingKey} = camelCase", result.Lines);
        Assert.Contains($"{StyleBro.Analyzers.Naming.HungarianNames.AllowedKey} = rx, db", result.Lines);
        Assert.Contains("stylebro_document_internal_elements = false", result.Lines);
        Assert.Contains("dotnet_diagnostic.IDE0073.severity = warning", result.Lines);
        Assert.Contains("dotnet_diagnostic.BRO1108.severity = none", result.Lines);
        Assert.Contains("SA1633", result.Covered);
    }

    [Fact]
    public void StyleSharp_WithStyleCop_ARuleIsOnWhenEitherEnforcesIt()
    {
        Write("A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleSharp.Analyzers\" Version=\"3.40.1\" /><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" /></ItemGroup></Project>\n");
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.SA1101.severity = none\ndotnet_diagnostic.SA1309.severity = none\ndotnet_diagnostic.SA1413.severity = none\n");

        var setup = StyleCopSetup.Read(root, _ => true);

        Assert.True(setup.StyleSharp!.WithStyleCop);
        Assert.True(setup.IsOn("SA1413")); // StyleSharp's SST1413 is on by default
        Assert.True(setup.IsOn("SA1116")); // StyleCop's default; no SST rule stands for it
        Assert.True(setup.IsOn("SX1309"));
        Assert.False(setup.IsOn("SA1309"));
        Assert.Contains(setup.Warnings, w => w.StartsWith("Both StyleCop.Analyzers and StyleSharp.Analyzers are referenced", StringComparison.Ordinal));
        Assert.Equal("StyleCop.Analyzers and StyleSharp.Analyzers", setup.PackageName);
    }

    [Fact]
    public void StyleSharp_SuppressionsCarryOver_ToTheReplacingRules()
    {
        Write("Repro.csproj", StyleSharpProject);
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.SST1309.severity = error\n");
        var setup = StyleCopSetup.Read(root, _ => true);
        var replacements = Suppressions.WithStyleSharp(Migration.Generate(setup, root).Replacements, setup);

        Assert.Contains("BRO1303", replacements["SST1309"]);
        Assert.Contains("IDE0003", replacements["SST1117"]); // omit_this: SX1101's rule
        Assert.DoesNotContain("SST1119", replacements.Keys); // stands for no StyleCop rule
        var (text, added) = Suppressions.Rewrite("#pragma warning disable SST1309 // ours\nclass C { [System.Diagnostics.CodeAnalysis.SuppressMessage(\"StyleSharp.Naming\", \"SST1309:Private fields\")] int x; }\n", replacements);
        Assert.Equal(2, added);
        Assert.Contains("#pragma warning disable SST1309, BRO1303", text, StringComparison.Ordinal);
        Assert.Contains("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"StyleBro\", \"BRO1303\")]", text, StringComparison.Ordinal);
        Assert.Equal(("<NoWarn>SST1309;BRO1303</NoWarn>", 1), Suppressions.RewriteNoWarn("<NoWarn>SST1309</NoWarn>", replacements));
    }

    [Fact]
    public void StyleSharp_Mapping_NamesOnlyStyleCopRules()
    {
        var styleCop = StyleCopSetup.Rules.Select(r => r.Id).ToHashSet();
        Assert.Equal(625, StyleCopSetup.StyleSharpRules.Count);
        Assert.All(StyleCopSetup.StyleSharpRules.SelectMany(r => r.StyleCop), id => Assert.Contains(id, styleCop));
        Assert.Equal(["SX1309", "SX1309S"], StyleCopSetup.StyleSharpRules.Single(r => r.Id == "SST1309").StyleCop);
        Assert.Empty(StyleCopSetup.StyleSharpRules.Single(r => r.Id == "SST1119").StyleCop);
        Assert.Equal(Severity.None, StyleCopSetup.StyleSharpRules.Single(r => r.Id == "SST1306").Default);
        Assert.Equal(Severity.Suggestion, StyleCopSetup.StyleSharpRules.Single(r => r.Id == "SST1220").Default);
    }

    [Fact]
    public void Init_PointsAStyleSharpRepository_ToTheMigration()
    {
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.SST1309.severity = error\nstylesharp.instance_member_qualification = omit_this\n");
        Write("Directory.Build.props", "<Project><ItemGroup><PackageReference Include=\"StyleSharp.Analyzers\" Version=\"3.40.1\" /></ItemGroup></Project>\n");

        Assert.Equal([".editorconfig (StyleSharp rule severities or settings)", "Directory.Build.props (StyleSharp.Analyzers package)"], StyleCopSetup.Evidence(root).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Migrate_ALeftoverStyleCopJsonNextToSonar_PointsToInit()
    {
        // Splat before StyleSharp was read: no StyleCop package, a stylecop.json, Sonar: StyleCop's defaults renamed 72 files.
        Write("Directory.Build.props", "<Project><ItemGroup><PackageReference Include=\"SonarAnalyzer.CSharp\" Version=\"10.31.0.145097\" /></ItemGroup></Project>\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("stylecop.json", "{ \"settings\": { } }\n");

        var output = Capture(() => Assert.Equal(1, Program.Migrate([root, "--write"])));

        Assert.Contains("Run 'stylebro-migrate init' instead: it follows the code's own conventions and the Sonar setup", output, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Migrate_GivesAnOptOutFolder_InitsOptOutBlock()
    {
        Write("A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" /></ItemGroup></Project>\n");
        Write("C.cs", "class C\n{\n}\n");
        Write("Samples/.editorconfig", OptOutFolder);
        Write("Samples/S.cs", "class S {\n  int x;\n}\n");

        var setup = StyleCopSetup.Read(root, _ => true);
        var main = Migration.Generate(setup, root);
        var plan = Migration.Plan(setup, root, main);

        var samples = Migration.Render(plan[Path.Combine("Samples", ".editorconfig")]);
        Assert.Contains(InitCommand.OptOutComment, samples, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.BRO1001.severity = none\n", samples, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.IDE0009.severity = none\n", samples, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none\n", samples, StringComparison.Ordinal);
        Assert.DoesNotContain("= warning", samples, StringComparison.Ordinal);
        Assert.Contains(main.Notes, n => n.StartsWith("Samples/.editorconfig turns every analyzer off", StringComparison.Ordinal));
    }

    [Fact]
    public void Migrate_NamesFilesNotInTheOrderOfTheRepositorysOwnSortKey()
    {
        // Splat: dotnet_sort_system_directives_first = true, with 'using Splat.Log4Net;' above 'using log4net;' as StyleSharp's
        // SST1210 wants; format's IMPORTS put log4net first and SST1210 failed the build.
        Write("A.csproj", StyleSharpProject);
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_sort_system_directives_first = true\n");
        Write("Sorted.cs", "using System;\nusing log4net;\n\nclass S { }\n");
        Write("Unsorted.cs", "using MyApp.Logging;\n\nusing log4net;\n\nclass U { }\n");

        var setup = StyleCopSetup.Read(root, _ => true);
        var note = Migration.UnsortedUnderOwnKey(setup, root, Migration.OwnSettings(root));

        Assert.NotNull(note);
        Assert.StartsWith("using directives: your .editorconfig sets dotnet_sort_system_directives_first = true, and 'dotnet format' sorts usings whenever it is set (to any value): 1 file isn't in that order (Unsorted.cs)", note, StringComparison.Ordinal);
        Assert.Contains("StyleSharp's SST1208/SST1210 (on here) keep another order", note, StringComparison.Ordinal);
        Assert.Contains(note, Migration.Generate(setup, root).Notes);
        File.Delete(Path.Combine(root, "Unsorted.cs"));
        Assert.Null(Migration.UnsortedUnderOwnKey(setup, root, Migration.OwnSettings(root)));
    }

    [Fact]
    public void Format_NamesNoFormatter_WhereIDE0055IsJustOff()
    {
        Assert.Equal("No whitespace pass: src/: IDE0055 is off there in the stylebro settings, so whitespace formatting leaves it alone; style and analyzer fixes still run there.", FormatCommand.NoWhitespaceLine(root, ["src/"]));
        Write(".csharpierrc.json", "{}\n");
        Assert.Contains("stylebro settings (CSharpier's code), so", FormatCommand.NoWhitespaceLine(root, ["src/"]), StringComparison.Ordinal);
    }

    [Fact]
    public void Format_SaysNothingAboutAGeneratedFileNoBuiltFrameworkCompiles()
    {
        // Splat.Drawing's Resources/Resource.designer.cs: compiled for Android only, which this machine didn't build.
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Compile Remove=\"Resources\\**\" /></ItemGroup></Project>\n");
        Write("src/Lib/Code.cs", "class C { }\n");
        Write("src/Lib/Form.Designer.cs", "class F { }\n");
        Write("src/Lib/Resources/Resource.designer.cs", "// <auto-generated>\nclass R { }\n");
        PreviewCommand.MakeRepository(root);
        var changed = new[] { Path.Combine(root, "src", "Lib", "Code.cs") };
        Assert.Equal(["src/Lib/Form.Designer.cs", "src/Lib/Resources/Resource.designer.cs"], FormatCommand.CheckedInGenerated(root, changed));

        _ = FormatCommand.EvaluatedProjects(Path.Combine(root, "src", "Lib", "Lib.csproj")).ToList();

        Assert.Equal(["src/Lib/Form.Designer.cs"], FormatCommand.CheckedInGenerated(root, changed));
    }
}
