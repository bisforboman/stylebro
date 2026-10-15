using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void Init_FromASubfolder_PinsThePresetInTheRootProps()
    {
        // Trial 22 (c8-csharpier-src): init from src/ wrote src/Directory.Build.props with only <StyleBroPreset>, which shadowed
        // the root's: every project lost StyleBro, TreatWarningsAsErrors and Nullable.
        Write(".git/HEAD", "ref: refs/heads/main\n");
        Write("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <Nullable>enable</Nullable>\n  </PropertyGroup>\n</Project>\n");
        Write("src/.editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("src/App.slnx", "<Solution>\n  <Project Path=\"Lib/Lib.csproj\" />\n</Solution>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib/Sink.cs", "namespace Lib;\n\npublic class Sink\n{\n    public void Flush()\n    {\n    }\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { Path.Combine(root, "src"), "--write" })));

        Assert.Contains("Working on the repository's root", output, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "src", "Directory.Build.props")));
        Assert.Contains("<StyleBroPreset>", File.ReadAllText(Path.Combine(root, "Directory.Build.props")), StringComparison.Ordinal);
        Assert.Contains("# BEGIN stylebro-migrate", File.ReadAllText(Path.Combine(root, "src", ".editorconfig")), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Init_FromTheSolutionsFolder_WritesWhereTheProjectsAre()
    {
        // Trial 22 (c9b-subsln-build): build/App.sln lists ../src and ../tests; init from build/ wrote build/.editorconfig and
        // build/Directory.Build.props, which no project reads, and judged the conventions from no code.
        Write(".git/HEAD", "ref: refs/heads/main\n");
        Write("build/App.slnx", "<Solution>\n  <Project Path=\"../src/Lib/Lib.csproj\" />\n</Solution>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib/Store.cs", "namespace Lib;\n\npublic class Store\n{\n    private int _a;\n    private int _b;\n    private int _c;\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { Path.Combine(root, "build"), "--write" })));

        Assert.Contains("App.slnx (lists projects outside it)", output, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "build", ".editorconfig")));
        Assert.False(File.Exists(Path.Combine(root, "build", "Directory.Build.props")));
        Assert.Contains("stylebro_private_field_naming = _camelCase", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);
        Assert.Contains("<StyleBroPreset>", File.ReadAllText(Path.Combine(root, "Directory.Build.props")), StringComparison.Ordinal);
    }

    [Fact]
    public void RepositoryFolder_StaysForASelfContainedFolder()
    {
        // A folder whose build doesn't reach past it stays the root, inside a repository or not.
        Write(".git/HEAD", "ref: refs/heads/main\n");
        Write("tools/Gen/Gen.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        string? workspace = null;
        Assert.Equal(Path.Combine(root, "tools"), Program.RepositoryFolder(Path.Combine(root, "tools"), ref workspace));

        // A nested props file that doesn't import its parent shadows it: the parent's doesn't count.
        Write("Directory.Build.props", "<Project />\n");
        Write("tools/Directory.Build.props", "<Project />\n");
        Assert.Equal(Path.Combine(root, "tools"), Program.RepositoryFolder(Path.Combine(root, "tools"), ref workspace));

        // A 'root = true' .editorconfig stops the lookup: the root's .editorconfig doesn't reach the folder.
        File.Delete(Path.Combine(root, "Directory.Build.props"));
        File.Delete(Path.Combine(root, "tools", "Directory.Build.props"));
        Write(".editorconfig", "root = true\n");
        Write("tools/.editorconfig", "root = true\n");
        Assert.Equal(Path.Combine(root, "tools"), Program.RepositoryFolder(Path.Combine(root, "tools"), ref workspace));
        File.Delete(Path.Combine(root, "tools", ".editorconfig"));

        // --project follows the move.
        workspace = "Gen/Gen.csproj";
        Capture(() => Assert.Equal(root, Program.RepositoryFolder(Path.Combine(root, "tools"), ref workspace)));
        Assert.Equal(Path.Combine("tools", "Gen", "Gen.csproj"), workspace);
    }

    [Fact]
    public void SetPreset_ImportsTheParentProps_InANewFile()
    {
        var created = Migration.SetPreset(null, "1.0", "comment", importParent: true)!;
        Assert.Contains("<Import Project=\"$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))\" />", created, StringComparison.Ordinal);
        Assert.DoesNotContain("<Import", Migration.SetPreset(null, "1.0", "comment")!, StringComparison.Ordinal);

        Write("Directory.Build.props", "<Project />\n");
        Assert.True(Migration.HasParentProps(Path.Combine(root, "src", "Directory.Build.props")));
        Assert.False(Migration.HasParentProps(Path.Combine(root, "Directory.Build.props")) && !File.Exists(Path.Combine(Path.GetDirectoryName(root)!, "Directory.Build.props")));
    }

    [Fact]
    public void Conventions_CountLeadingLambdas_EmptyBlocks_OneLineProperties_AndPartialParts()
    {
        // CommunityToolkit: 'Register(c =>' + '}, Kind);' (owner's choice 2026-10-15), 'partial class Guard' parts (BRO1007);
        // akka.net: '{ }' and 'X { get { return x; } }' on one line (BRO1508/BRO1509), mixed accessor bodies (BRO1510).
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            using System;

            public partial class Guard
            {
                private int x;

                public int X { get { return x; } }

                public int Y
                {
                    get { return x; }
                    set
                    {
                        x = value;
                    }
                }

                public int Z
                {
                    get
                    {
                        return x;
                    }
                }

                public int W
                {
                    get { return x; }
                    set => x = value;
                }

                void M(Action<int> a, int kind)
                {
                    M(c =>
                    {
                        Console.WriteLine(c);
                    }, 1);
                    M(
                        c =>
                        {
                        },
                        2);
                    M(
                        c =>
                        {
                        }, 3);
                    try { } catch { }
                    if (kind > 0)
                    {
                    }
                }
            }

            partial class Guard
            {
            }

            internal class Helper
            {
            }
            """),
            counts);

        Assert.Equal(new[] { 1, 1 }, counts[StyleBro.Analyzers.Readability.ParameterLayout.LeadingLambdaKey]);
        Assert.Equal(new[] { 1, 2 }, counts[StyleBro.Analyzers.Layout.SingleLineBlocks.AllowEmptyKey]);
        Assert.Equal(new[] { 3, 1 }, counts["dotnet_diagnostic.BRO1509.severity"]);
        Assert.Equal(new[] { 0, 1 }, counts["dotnet_diagnostic.BRO1510.severity"]);
        Assert.Equal(new[] { 1, 1 }, counts["dotnet_diagnostic.BRO1007.severity"]);

        // Clear majorities: the option is written, the rules go off.
        counts = Conventions.NewCounts();
        counts[StyleBro.Analyzers.Readability.ParameterLayout.LeadingLambdaKey] = [1, 50];
        counts[StyleBro.Analyzers.Layout.SingleLineBlocks.AllowEmptyKey] = [3, 1004];
        counts["dotnet_diagnostic.BRO1509.severity"] = [20, 271];
        counts["dotnet_diagnostic.BRO1510.severity"] = [2, 8];
        counts["dotnet_diagnostic.BRO1007.severity"] = [10, 74];
        var (lines, _) = Conventions.Decide(counts, new Dictionary<string, string>());
        Assert.Contains("stylebro_allow_leading_multiline_lambda = true", lines);
        Assert.Contains("stylebro_allow_empty_single_line_blocks = true", lines);
        Assert.Contains("dotnet_diagnostic.BRO1509.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.BRO1510.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.BRO1007.severity = none", lines);

        // Mixed: both layouts allowed (an option) or the rule off; the StyleCop layout everywhere: nothing written.
        counts[StyleBro.Analyzers.Readability.ParameterLayout.LeadingLambdaKey] = [10, 10];
        counts[StyleBro.Analyzers.Layout.SingleLineBlocks.AllowEmptyKey] = [40, 0];
        counts["dotnet_diagnostic.BRO1007.severity"] = [40, 1];
        (lines, _) = Conventions.Decide(counts, new Dictionary<string, string>());
        Assert.Contains("stylebro_allow_leading_multiline_lambda = true", lines);
        Assert.DoesNotContain(lines, l => l.StartsWith("stylebro_allow_empty_single_line_blocks", StringComparison.Ordinal));
        Assert.DoesNotContain("dotnet_diagnostic.BRO1007.severity = none", lines);
    }

    [Fact]
    public void Migrate_SpacingRulesOff_KeepTheCodesSpacing_OrNameWhatFormatChanges()
    {
        // kafkaflow (StyleCop 1.1.118, ruleset: SA1000, SA1003, SA1009 off): IDE0055 rewrote 'new ()' and 'catch(' StyleCop
        // never checked. A clear other form gets the SDK option; mixed operators 'ignore'; no option for 'new ()': a note.
        Write("Lib.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.1.118" /></ItemGroup></Project>""");
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.SA1000.severity = none\ndotnet_diagnostic.SA1003.severity = none\n");
        Write("C.cs", """
            using System.Collections.Generic;

            public class C
            {
                private readonly List<int> a = new ();
                private readonly List<int> b = new();

                public int M(int x)
                {
                    if(x > 0) { x = x+1; }
                    if(x > 1) { x = x + 1; }
                    while(x > 2) { x = x-1; }
                    foreach(var i in a) { x = x * 2; }
                    return x;
                }
            }
            """);

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("dotnet_diagnostic.IDE0055.severity = warning", result.Lines);
        Assert.Contains("csharp_space_after_keywords_in_control_flow_statements = false", result.Lines);
        Assert.Contains("csharp_space_around_binary_operators = ignore", result.Lines);
        Assert.Contains(result.Notes, n => n.Contains("'format' changes target-typed 'new' (1 'new()', 1 'new ()'; SA1000 off)", StringComparison.Ordinal));

        // StyleCop's rules on: nothing counted, nothing written.
        Write(".editorconfig", "root = true\n");
        result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("csharp_space_a", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Notes, n => n.Contains("IDE0055 also enforces spacing", StringComparison.Ordinal));
    }
}
