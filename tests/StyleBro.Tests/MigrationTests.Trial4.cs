using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What a trial of 0.4.0-alpha.1 on Bogus, RealWorld, Buildalyzer and FluentAssertions found in stylebro-migrate.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Init_TurnsTheRuleOff_WithoutAClearMajority_LikeBogus()
    {
        // Bogus: 22 'is null' and 50 '== null'; BRO1133 made 35 edits with StyleBro's default.
        Write("A.cs", "class A\n{\n    bool M(object a) => " + string.Join(" || ", Enumerable.Range(0, 6).Select(_ => "a is null").Concat(Enumerable.Range(0, 6).Select(_ => "a == null"))) + ";\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("dotnet_diagnostic.BRO1133.severity = none", editorConfig);
        Assert.DoesNotContain("stylebro_null_check_style =", editorConfig);
        Assert.Contains("null checks: 6 'is null', 6 '== null'; no clear majority, so StyleBro doesn't enforce either form: dotnet_diagnostic.BRO1133.severity = none", output);

        // The repository's own severity stays.
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.BRO1133.severity = warning\n");
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.DoesNotContain("BRO1133.severity = none", File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Init_FollowsFewPlacesThatAllAgree_LikeRealWorld()
    {
        // RealWorld: its only 2 private fields are '_logger' and '_mediator'; both were renamed.
        Write("A.cs", "class A { private int _logger; private int _mediator; }");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("stylebro_private_field_naming = _camelCase", File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains("stylebro_private_field_naming = _camelCase (few places, all 2 agree)", output);

        // Few places that disagree: the rule is off.
        Write("A.cs", "class A { private int _logger; private int mediator; }");
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Contains("dotnet_diagnostic.BRO1303.severity = none", File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Conventions_CountArithmeticParentheses()
    {
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class C
            {
                void M(int a, int b, int c)
                {
                    var x1 = a + b * c;
                    var x2 = a + (b * c);
                    var x3 = (a * b) % c;
                    var x4 = (a + b) * c;
                    var x5 = a * (b % c);
                    var x6 = a << b + c;
                    var x7 = a + b - c;
                }
            }
            """),
            counts);

        // With: 'a + (b * c)', '(a * b) % c'. Without: 'a + b * c', 'a << b + c'. Needed parentheses and one family don't count.
        Assert.Equal(new[] { 2, 2 }, counts["dotnet_style_parentheses_in_arithmetic_binary_operators"]);
        Assert.Contains("dotnet_diagnostic.BRO1406.severity = none", Conventions.Decide(new Dictionary<string, int[]> { ["dotnet_style_parentheses_in_arithmetic_binary_operators"] = new[] { 20, 40 } }, new Dictionary<string, string>()).Lines);
        Assert.Contains("dotnet_style_parentheses_in_arithmetic_binary_operators = never_if_unnecessary", Conventions.Decide(new Dictionary<string, int[]> { ["dotnet_style_parentheses_in_arithmetic_binary_operators"] = new[] { 1, 50 } }, new Dictionary<string, string>()).Lines);
    }

    [Fact]
    public void ThePackage_GoesWhereEveryProjectImportsIt_LikeBogus()
    {
        // Bogus: Source/Directory.Build.props doesn't import the root's, so a reference there never loaded.
        Write("Source/Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <LangVersion>14.0</LangVersion>\n  </PropertyGroup>\n</Project>\n");
        Write("Source/Bogus/Bogus.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("Source/Builder/Directory.Build.props", "<Project>\n  <!--<Import Project=\"$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))\" />-->\n</Project>\n");
        Write("Source/Builder/Builder.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("Source/Imports/Directory.Build.props", "<Project>\n  <Import Project=\"$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))\" />\n</Project>\n");
        Write("Source/Imports/Imports.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("Tools/Tool.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var expected = new[] { "Directory.Build.props", "Source/Builder/Directory.Build.props", "Source/Directory.Build.props" };
        Assert.Equal(expected, Migration.PropsFiles(root).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')));

        Assert.NotNull(PreviewCommand.AddPackage(root));
        foreach (var file in expected)
        {
            Assert.Contains("<PackageReference Include=\"StyleBro.Analyzers\"", File.ReadAllText(Path.Combine(root, file)));
        }

        Assert.DoesNotContain("StyleBro.Analyzers", File.ReadAllText(Path.Combine(root, "Source/Imports/Directory.Build.props")));
        Assert.Null(PreviewCommand.AddPackage(root)); // referenced now
    }

    [Fact]
    public void ThePreview_WarnsWhenStyleBroReportedNothing()
    {
        static string Report(string id) => $$"""[ { "FilePath": "a.cs", "FileChanges": [ { "LineNumber": 1, "CharNumber": 1, "DiagnosticId": "{{id}}" } ] } ]""";

        Assert.Contains("didn't load", PreviewCommand.NotLoadedWarning(Report("IDE0055")));
        Assert.Null(PreviewCommand.NotLoadedWarning(Report("BRO1520")));
    }

    [Fact]
    public void Format_NamesTheProjectsDotnetFormatSkipped()
    {
        Assert.Equal("Bogus", FormatCommand.SkippedProject("Required references did not load for Bogus or referenced project. Run `dotnet restore` prior to formatting."));
        Assert.Null(FormatCommand.SkippedProject("Warnings were encountered while loading the workspace."));

        // Bogus: skipped for netstandard1.3 only, its other frameworks' runs formatted it; a broken cache skips everything.
        var frameworks = new Dictionary<string, List<string>> { ["Bogus"] = new() { "net6.0", "netstandard1.3" }, ["Tests"] = new() { "net6.0" } };
        var partly = FormatCommand.Incomplete(new Dictionary<string, SortedSet<string>> { ["Bogus"] = new() { "netstandard1.3" } }, frameworks);
        Assert.Equal(new[] { "Partly incomplete: 'dotnet format' skipped Bogus for netstandard1.3 (references didn't load): code only that framework compiles (inside #if) wasn't formatted." }, partly);
        var whole = FormatCommand.Incomplete(new Dictionary<string, SortedSet<string>> { ["Bogus"] = new() { "net6.0", "netstandard1.3" }, ["Tests"] = new() { "net6.0" } }, frameworks);
        Assert.StartsWith(FormatCommand.IncompleteMarker + " 'dotnet format' skipped Bogus, Tests", whole[0]);
        Assert.StartsWith(FormatCommand.IncompleteMarker, FormatCommand.Incomplete(new Dictionary<string, SortedSet<string>> { ["App"] = new() { string.Empty } }, new Dictionary<string, List<string>>())[0]);
    }

    [Fact]
    public void PreviewSamples_DescribeInvisibleChanges()
    {
        Assert.Equal(new[] { "(adds the final newline)" }, PreviewCommand.Hunk("@@ -10 +10 @@\n-}\n\\ No newline at end of file\n+}\n", 10, 12));
        Assert.Equal(new[] { "(removes the final newline)" }, PreviewCommand.Hunk("@@ -10 +10 @@\n-}\n+}\n\\ No newline at end of file\n", 10, 12));
        Assert.Equal(new[] { "(adds a byte order mark)" }, PreviewCommand.Hunk("@@ -1 +1 @@\n-using A;\n+﻿using A;\n", 1, 12));
        Assert.Equal(new[] { "(removes a blank line)" }, PreviewCommand.Hunk("@@ -5 +4,0 @@\n-\n", 5, 12));
        Assert.Equal(new[] { "(adds 2 blank lines)" }, PreviewCommand.Hunk("@@ -4,0 +5,2 @@\n+\n+\n", 4, 12));
        Assert.Equal(new[] { "(removes trailing whitespace)" }, PreviewCommand.Hunk("@@ -4 +4 @@\n-x;  \n+x;\n", 4, 12));
        Assert.Equal(new[] { "-x", "+y" }, PreviewCommand.Hunk("@@ -4 +4 @@\n-x\n+y\n", 4, 12));
    }

    [Fact]
    public void Init_MarksVendoredCodeAsGenerated_AndKnowsEnforceCodeStyleInBuild()
    {
        // Bogus: Source/Bogus/Vendor/ was skipped when counting but reformatted.
        Write("Source/Bogus/Vendor/Lib.cs", "class L { }");
        Write("Source/Bogus/Vendor/Nested/More.cs", "class M { }");
        Write("Source/Bogus/A.cs", "class A { }");

        Assert.Equal(new[] { "Source/Bogus/Vendor" }, Migration.VendoredFolders(root));
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Contains("[Source/Bogus/Vendor/**]\ngenerated_code = true", File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains("Vendored code in Source/Bogus/Vendor", output);
        Assert.Contains("EnforceCodeStyleInBuild", output);

        // The repository already sets it: no hint.
        File.Delete(Path.Combine(root, ".editorconfig"));
        Write("Directory.Build.props", "<Project><PropertyGroup><EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild></PropertyGroup></Project>");
        Assert.DoesNotContain("EnforceCodeStyleInBuild", Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }))));
    }
}
