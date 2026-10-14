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
    public void MSBuildEdits_FollowTheFilesIndentation_AndTheAnalyzersItemGroup()
    {
        Assert.Equal("<Project>\n\t<A />\n\t<ItemGroup>\n\t\t<X />\n\t</ItemGroup>\n</Project>", PreviewCommand.AddItem("<Project>\n\t<A />\n</Project>", "<X />"));
        Assert.Equal("<Project>\n    <PropertyGroup>\n        <StyleBroPreset>1.0</StyleBroPreset>\n    </PropertyGroup>\n    <A />\n</Project>", Migration.SetPreset("<Project>\n    <A />\n</Project>", "1.0", "c"));

        // AngleSharp: StyleBro's version went into the labelled group of the package's only shipped dependency.
        const string Packages = "<Project>\n  <ItemGroup Label=\"Shipped\">\n    <PackageVersion Include=\"System.Text.Encoding.CodePages\" Version=\"8\" />\n  </ItemGroup>\n  <ItemGroup Label=\"Build\">\n    <PackageVersion Include=\"ConfigureAwaitChecker.Analyzer\" Version=\"5\" />\n    <PackageVersion Include=\"Nullable\" Version=\"1\" />\n  </ItemGroup>\n</Project>\n";
        Assert.Contains("\"Nullable\" Version=\"1\" />\n    <PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />\n  </ItemGroup>", PreviewCommand.AddPackageVersion(Packages, "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />"), StringComparison.Ordinal);
    }
}
