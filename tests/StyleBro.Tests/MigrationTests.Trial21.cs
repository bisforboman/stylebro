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
                            default:
                            {
                                break;
                            }
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
        Assert.Equal(new[] { 1, 2 }, counts["csharp_indent_case_contents_when_block"]);

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
    public void Init_RunAgain_KeepsBothAccessModifierVerdicts()
    {
        // Members mixed and types without 'internal' both turn BRO1404 off: one line, and a second init changes nothing.
        for (var i = 0; i < 6; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n    {(i % 2 == 0 ? "private " : string.Empty)}int x;\n}}\n");
        }

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Single(first.Replace("\r\n", "\n").Split('\n'), l => l == "dotnet_diagnostic.BRO1404.severity = none");
        Assert.Contains("# init: private-by-default members:", first, StringComparison.Ordinal);

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));

        // Members with 'private', types without 'internal': the types' off line isn't the members' verdict on a second init.
        for (var i = 0; i < 6; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n    private int x;\n}}\n");
        }

        File.Delete(Path.Combine(root, ".editorconfig"));
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var again = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Contains("  default  private-by-default members:", again, StringComparison.Ordinal);
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
    public void GeneratedFileNote_LeavesDesignersOut_AndMentionsOtherProjects()
    {
        // SSH.NET: a resource designer's and a WinForms form's Designer.cs were named, which no build rewrites; gencheck2:
        // Lib2's Docs2.g.cs, regenerated from Lib's sources, wasn't.
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib/Code.cs", "class C { }\n");
        Write("src/Lib/Resources.resx", "<root />\n");
        Write("src/Lib/Resources.Designer.cs", "// <auto-generated>\nclass R { }\n");
        Write("src/Lib/Form1.cs", "partial class Form1 { }\n");
        Write("src/Lib/Form1.Designer.cs", "partial class Form1 { void InitializeComponent() { } }\n");
        Write("src/Lib/Docs.g.cs", "class D { }\n");
        Write("src/Lib2/Lib2.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib2/Docs2.g.cs", "class D2 { }\n");
        PreviewCommand.MakeRepository(root);

        var changed = new[] { Path.Combine(root, "src", "Lib", "Code.cs") };
        Assert.Equal(["src/Lib/Docs.g.cs"], FormatCommand.CheckedInGenerated(root, changed));
        Assert.Equal("Other projects have generated files checked in (src/Lib2/Docs2.g.cs): a build may regenerate them from the changed code too, so rebuild and check.", FormatCommand.AfterNotes(root, changed)[^1]);
    }

    [Fact]
    public void Texts_SayWhatHappens_InOptOutFolders_AndWithCodeStyleInTheBuild()
    {
        Write("src/Legacy/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = none\n");
        Write("src/Pretty/.editorconfig", "[*.cs]\nindent_size = 2\n");
        var line = FormatCommand.NoWhitespaceLine(root, ["src/Legacy/", "src/Pretty/"]);
        Assert.Contains("src/Pretty/: IDE0055 is off there", line, StringComparison.Ordinal);
        Assert.Contains("src/Legacy/: its .editorconfig turns every analyzer off, so 'format' leaves it alone.", line, StringComparison.Ordinal);
        Assert.DoesNotContain("src/Legacy/, ", line, StringComparison.Ordinal);

        // c6-acc-warn: EnforceCodeStyleInBuild is on, so "nothing enforces it in the build" was wrong.
        var counts = Conventions.NewCounts();
        counts["interface member access"] = [0, 24];
        var always = new Dictionary<string, string> { ["dotnet_style_require_accessibility_modifiers"] = "always:warning" };
        Assert.Contains("IDE0040 is off in init's block", Conventions.InterfaceAccess(counts, always, enforcedInBuild: false, codeStyleInBuild: true)?.Report, StringComparison.Ordinal);
        Assert.Contains("nothing enforces it in the build", Conventions.InterfaceAccess(counts, always, enforcedInBuild: false)?.Report, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_CountsSuppressionIdsAdded_LikeTheMigration()
    {
        // SSH.NET: the preview said 25 #pragma (lines), --write 27 (ids: 'SA1300' got BRO1309 and BRO1312).
        Write("A.cs", "#pragma warning disable SA1300\nclass a\n{\n}\n#pragma warning restore SA1300\n");
        PreviewCommand.MakeRepository(root);
        var from = PreviewCommand.GitChecked(root, "rev-parse", "HEAD").Trim();
        Write("A.cs", "#pragma warning disable SA1300, BRO1309, BRO1312\nclass a\n{\n}\n#pragma warning restore SA1300, BRO1309, BRO1312\n");
        PreviewCommand.GitChecked(root, "add", "-A");
        PreviewCommand.GitChecked(root, "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-q", "-m", "settings");

        Assert.Contains("suppressions carried over: 4 #pragma", PreviewCommand.SettingsSummary(root, from, "HEAD"), StringComparison.Ordinal);
    }

    [Fact]
    public void Migrate_SaysStyleCopJsonCanGo_OnceStyleCopIsGone()
    {
        Write("stylecop.json", """{ "settings": { "documentationRules": { "xmlHeader": false } } }""");
        var output = Capture(() => Assert.Equal(0, Program.Migrate(new[] { root })));
        Assert.Contains("stylecop.json: StyleBro doesn't read it; the settings are in the block now, which also records them for later runs. Once StyleCop.Analyzers is gone, it can go", output, StringComparison.Ordinal);
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
