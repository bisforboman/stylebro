using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void Init_WritesCSharpierLines_IntoARootTrueFolderBelowCSharpiersFolder()
    {
        // AzureEventGridSimulator: CSharpier in .config/dotnet-tools.json at the top, the code under src/, whose .editorconfig
        // says root = true. Only the root's .editorconfig (read by no C# file) got the CSharpier lines; src/'s turned IDE0055 on.
        Write(".git/HEAD", "ref: refs/heads/main\n");
        Write(".config/dotnet-tools.json", "{ \"tools\": { \"csharpier\": { \"version\": \"1.3.0\" } } }\n");
        Write(".editorconfig", "root = true\n\n[*]\nindent_style = space\n");
        Write("src/.editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("src/Directory.Build.props", "<Project>\n    <PropertyGroup>\n        <Nullable>enable</Nullable>\n    </PropertyGroup>\n</Project>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib/Sink.cs", "namespace Lib;\n\npublic class Sink\n{\n    public void Flush() { }\n}\n");

        Assert.Equal([("../.config/dotnet-tools.json", string.Empty)], Conventions.CSharpierScopes(Path.Combine(root, "src")));
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var src = File.ReadAllText(Path.Combine(root, "src", ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none\n", src, StringComparison.Ordinal);
        Assert.Contains("stylebro_layout_by = csharpier\n", src, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0055.severity = warning", src, StringComparison.Ordinal);

        // The preset version in the file's own indentation (CSharpier checks .props files too).
        Assert.Contains("    <PropertyGroup>\n        <StyleBroPreset>", File.ReadAllText(Path.Combine(root, "src", "Directory.Build.props")).Replace("\r\n", "\n"), StringComparison.Ordinal);

        // Format's note after a run from src/.
        Assert.Contains(FormatCommand.AfterNotes(Path.Combine(root, "src"), [Path.Combine(root, "src", "Lib", "Sink.cs")]), n => n.StartsWith("CSharpier formats", StringComparison.Ordinal));
    }

    [Fact]
    public void Verify_IsNotClean_WhenAFindingNoFixChangesIsLeft()
    {
        // AngleSharp: IDE0049 in a using alias, which the SDK's fix can't rewrite: verify printed three errors, then "Clean".
        Write("C.cs", "class C\n{\n}\n");
        var finding = Path.Combine(root, "C.cs") + "(1,7): error IDE0049: Use framework type [" + Path.Combine(root, "A.csproj") + "]";
        var lines = new List<string>();
        int Run(string[] args, Action<string>? output)
        {
            output?.Invoke(finding);
            output?.Invoke(finding);
            return args.Contains("--verify-no-changes") ? FormatCommand.NotCleanExitCode : 0;
        }

        Assert.Equal(FormatCommand.NotCleanExitCode, FormatCommand.Verify(new[] { "--verify-no-changes" }, root, lines.Add, Run));
        Assert.StartsWith("Not clean: 1 finding above no fix changes", lines[^1], StringComparison.Ordinal);
        Assert.DoesNotContain(lines, l => l.StartsWith("Clean", StringComparison.Ordinal));
    }

    [Fact]
    public void Conventions_CountTypesWithoutInternal_AndCaseBlocksAtTheLabel()
    {
        // AngleSharp: 273 types without an access modifier (26 'internal'), and 131 of 131 'case' blocks at the label's
        // indentation; BRO1404 added 'internal' everywhere and IDE0055 re-indented thousands of lines.
        var counts = Conventions.NewCounts();
        Conventions.Count(
            Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("""
            namespace N
            {
                class A
                {
                    class Nested { }

                    void M(int x)
                    {
                        switch (x)
                        {
                            case 1:
                            {
                                break;
                            }
                            case 2:
                                {
                                    break;
                                }
                            case 3: { break; }
                        }
                    }
                }

                internal class B { }
                public class C { }
                partial class D { }
                delegate void E();
            }
            """),
            counts);

        Assert.Equal(new[] { 1, 2 }, counts["dotnet_diagnostic.BRO1404.severity"]);
        Assert.Equal(new[] { 1, 1 }, counts["csharp_indent_case_contents_when_block"]);

        counts = Conventions.NewCounts();
        counts["dotnet_diagnostic.BRO1404.severity"] = [1, 20];
        counts["csharp_indent_case_contents_when_block"] = [0, 12];
        counts["dotnet_style_require_accessibility_modifiers"] = [10, 10];
        var (lines, _) = Conventions.Decide(counts, new Dictionary<string, string>());
        Assert.Contains("csharp_indent_case_contents_when_block = false", lines);
        Assert.Single(lines, "dotnet_diagnostic.BRO1404.severity = none");
        Assert.Equal(lines.Count, lines.Distinct().Count());
    }

    [Fact]
    public void NotBuiltProjects_AreNamedWithHowToFormatThem()
    {
        // AngleSharp: build/_build.csproj gets StyleBro from build/Directory.Build.props, no solution configuration builds it,
        // and its ~25 warnings stayed after every format run.
        Write("build/_build.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Assert.Equal(" Where a Directory.Build.props above brings StyleBro into its build, format it on its own: 'stylebro-migrate format build/_build.csproj'.", FormatCommand.NotBuiltHint(["build/"], root));
        Assert.Equal(string.Empty, FormatCommand.NotBuiltHint([], root));
    }

    [Fact]
    public void Format_RemovesUsingsItsFixesLeftUnused_InTheSameRun()
    {
        // ide5: 'Int32' -> 'int' left 'using System;' unused, and IDE0005 removed it only in a run of its own (3 runs).
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_diagnostic.IDE0005.severity = warning\n");
        Write("C.cs", "using System;\n\nclass C\n{\n    Int32 x;\n}\n");
        var calls = new List<string[]>();
        int Run(string[] args, Action<string>? output)
        {
            calls.Add(args);
            if (calls.Count == 1)
            {
                Write("C.cs", "using System;\n\nclass C\n{\n    int x;\n}\n");
            }
            else if (args.Contains("IDE0005"))
            {
                Write("C.cs", "class C\n{\n    int x;\n}\n");
            }

            return 0;
        }

        var lines = new List<string>();
        Assert.Equal(0, FormatCommand.Run(new[] { root }, lines.Add, Run));

        Assert.Equal(3, calls.Count);
        Assert.Contains("IDE0005", calls[1]);
        Assert.Contains("Run 2: 0 files changed, clean.", lines);
    }

    [Fact]
    public void MSBuildEdits_FollowTheFilesIndentation_AndTheAnalyzersItemGroup()
    {
        Assert.Equal("<Project>\n\t<A />\n\t<ItemGroup>\n\t\t<X />\n\t</ItemGroup>\n</Project>", PreviewCommand.AddItem("<Project>\n\t<A />\n</Project>", "<X />"));
        Assert.Equal("<Project>\n    <PropertyGroup>\n        <StyleBroPreset>1.0</StyleBroPreset>\n    </PropertyGroup>\n    <A />\n</Project>", Migration.SetPreset("<Project>\n    <A />\n</Project>", "1.0", "c"));

        // AngleSharp: StyleBro's version went into the labelled group of the package's only shipped dependency.
        const string Packages = "<Project>\n  <ItemGroup Label=\"Shipped\">\n    <PackageVersion Include=\"System.Text.Encoding.CodePages\" Version=\"8\" />\n  </ItemGroup>\n  <ItemGroup Label=\"Build\">\n    <PackageVersion Include=\"ConfigureAwaitChecker.Analyzer\" Version=\"5\" />\n    <PackageVersion Include=\"Nullable\" Version=\"1\" />\n  </ItemGroup>\n</Project>\n";
        Assert.Contains("\"Nullable\" Version=\"1\" />\n    <PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />\n  </ItemGroup>", PreviewCommand.AddPackageVersion(Packages, "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />"), StringComparison.Ordinal);
    }
}
