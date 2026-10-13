using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void Init_SetsStyleBroRulesById_WhereABulkSeverityHidesThemFromFormat()
    {
        // sharpcompress: 'dotnet_analyzer_diagnostic.severity = silent' hid every rule from 'dotnet format' ("clean"),
        // while the build took the preset's severities and failed with ~40,000 errors.
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_analyzer_diagnostic.severity = silent\ndotnet_diagnostic.BRO1002.severity = none\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/C.cs", "class C\n{\n}\n");
        Assert.StartsWith("Not clean: .editorconfig's bulk severity", FormatCommand.HiddenLine(Path.Combine(root, "App")), StringComparison.Ordinal);

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var text = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains(InitCommand.ByIdComment, text, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.BRO1001.severity = warning\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.BRO1002.severity = warning", text, StringComparison.Ordinal);
        Assert.Null(FormatCommand.HiddenLine(Path.Combine(root, "App")));

        // A migration sets every rule by id (preset none): nothing is hidden there.
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_analyzer_diagnostic.severity = silent\n");
        Write("Directory.Build.props", "<Project><PropertyGroup><StyleBroPreset>none</StyleBroPreset></PropertyGroup></Project>\n");
        Assert.Null(FormatCommand.HiddenLine(root));
    }

    [Fact]
    public void Format_IsNotClean_WhenABulkSeverityHidesRules()
    {
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_analyzer_diagnostic.severity = silent\n");
        Write("C.cs", "class C\n{\n}\n");
        var lines = new List<string>();

        var code = FormatCommand.Run(new[] { root, FormatCommand.OnceOption }, lines.Add, (_, _) => 0);

        Assert.Equal(FormatCommand.NotCleanExitCode, code);
        Assert.Contains(lines, l => l.StartsWith("Not clean: .editorconfig's bulk severity", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.EndsWith(", clean.", StringComparison.Ordinal) || l.StartsWith("Clean", StringComparison.Ordinal));
    }

    [Fact]
    public void HiddenFromFormat_CountsCategories_AndOnlySeveritiesBelowWarning()
    {
        var naming = InitCommand.HiddenFromFormat(new Dictionary<string, string> { ["dotnet_analyzer_diagnostic.category-Naming.severity"] = "silent" });
        Assert.Contains("dotnet_diagnostic.BRO1301.severity = warning", naming);
        Assert.DoesNotContain(naming, l => l.StartsWith("dotnet_diagnostic.BRO1001.", StringComparison.Ordinal));

        Assert.Empty(InitCommand.HiddenFromFormat(new Dictionary<string, string> { ["dotnet_analyzer_diagnostic.severity"] = "error" }));
        Assert.NotEmpty(InitCommand.HiddenFromFormat(new Dictionary<string, string> { ["dotnet_analyzer_diagnostic.severity"] = "suggestion" }));

        // A category's own severity wins over the bulk one, like in the compiler.
        var bulk = InitCommand.HiddenFromFormat(new Dictionary<string, string>
        {
            ["dotnet_analyzer_diagnostic.severity"] = "none",
            ["dotnet_analyzer_diagnostic.category-Naming.severity"] = "warning",
        });
        Assert.DoesNotContain("dotnet_diagnostic.BRO1301.severity = warning", bulk);
        Assert.Contains("dotnet_diagnostic.BRO1001.severity = warning", bulk);
    }

    [Fact]
    public void CSharpierScopes_FindsEveryFolder()
    {
        // synth1: only src/Pretty/ was found, and BRO1110 fought CSharpier in src/Pretty2/ forever.
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib/A.cs", "class A\n{\n}\n");
        Write("src/Pretty/Pretty.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Pretty/.csharpierrc.yaml", "printWidth: 100\n");
        Write("src/Pretty/B.cs", "class B\n{\n}\n");
        Write("src/Pretty2/Pretty2.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Pretty2/.csharpierrc.yaml", "printWidth: 120\n");
        Write("src/Pretty2/C.cs", "class C\n{\n}\n");

        Assert.Equal([("src/Pretty/.csharpierrc.yaml", "src/Pretty"), ("src/Pretty2/.csharpierrc.yaml", "src/Pretty2")], Conventions.CSharpierScopes(root));
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var text = File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains("[src/Pretty/**.cs]\n", text, StringComparison.Ordinal);
        Assert.Contains("[src/Pretty2/**.cs]\n", text, StringComparison.Ordinal);
        Assert.Contains("stylebro_layout_print_width = 120", text, StringComparison.Ordinal);

        // The whitespace pass leaves both folders out.
        Assert.Equal(["src/Pretty/", "src/Pretty2/"], FormatCommand.WhitespaceExclusions(root));

        // One that covers everything wins.
        Write(".csharpierrc", "{}\n");
        Assert.Equal([(".csharpierrc", string.Empty)], Conventions.CSharpierScopes(root));
    }

    [Fact]
    public void Migrate_NamesAStyleCopReferenceInASubmodule_AndRecognisesTheSwap()
    {
        // Fonts: StyleCop came from shared-infrastructure's props, which the report didn't name; after the swap a second
        // --write called everything leftovers and pointed to init.
        Write("shared/.git", "gitdir: ../.git/modules/shared\n");
        Write("shared/Global.props", "<Project><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" /></ItemGroup></Project>\n");
        Write("Directory.Build.props", "<Project><Import Project=\"shared/Global.props\" /></Project>\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/C.cs", "class C\n{\n}\n");

        var setup = StyleCopSetup.Read(root, _ => true);
        Assert.Equal(["shared/Global.props"], setup.ReferencedOutside);
        var hint = Assert.Single(Program.PackageHints(setup, root));
        Assert.StartsWith("StyleCop.Analyzers is referenced in shared/Global.props, outside this repository's own files (a git submodule)", hint, StringComparison.Ordinal);
        Assert.Contains("<PackageReference Remove=\"StyleCop.Analyzers\" />", hint, StringComparison.Ordinal);
        Assert.Equal(Program.NextStep, Program.NextAfterWrite(setup, root));

        // The swap in the repository's own props, after a --write that recorded the version: migrated, not leftovers.
        Write("Directory.Build.props", "<Project><Import Project=\"shared/Global.props\" /><ItemGroup><PackageReference Remove=\"StyleCop.Analyzers\" /><PackageReference Include=\"StyleBro.Analyzers\" Version=\"1.0.0\" PrivateAssets=\"all\" /></ItemGroup></Project>\n");
        Write(".editorconfig", $"root = true\n{Migration.BeginMarker}\n[*.cs]\n{Migration.VersionPrefix}1.2.0-beta.556 (x)\n{Migration.EndMarker}\n");
        setup = StyleCopSetup.Read(root, _ => false);
        Assert.False(setup.NotInstalled);
        Assert.Empty(Program.PackageHints(setup, root));
        Assert.Equal(Program.FormatHint, Program.NextAfterWrite(setup, root));
    }

    [Fact]
    public void Init_FollowsInterfaceMembersWithoutModifiers_WhenNothingEnforcesAlways()
    {
        // BaGetter: 'always:warning' (IDE-only), 0 of 102 interface members with one, and BRO1404 added 'public' to all.
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_style_require_accessibility_modifiers = always:warning\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/I.cs", "interface I\n{\n    void A();\n\n    int B { get; }\n\n    void C();\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("interface members: your .editorconfig says dotnet_style_require_accessibility_modifiers = always:warning, but your code has 3 of 3 interface members without one", output, StringComparison.Ordinal);
        Assert.Contains("\ndotnet_style_require_accessibility_modifiers = for_non_interface_members\n", File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n"), StringComparison.Ordinal);

        // Code that follows 'always', or too little code to tell: the key stays; mixed: interface members are left alone.
        var counts = Conventions.NewCounts();
        var always = new Dictionary<string, string> { ["dotnet_style_require_accessibility_modifiers"] = "always" };
        counts["interface member access"] = [3, 0];
        Assert.Null(Conventions.InterfaceAccess(counts, always, enforcedInBuild: false));
        counts["interface member access"] = [0, 2];
        Assert.Null(Conventions.InterfaceAccess(counts, always, enforcedInBuild: false));
        counts["interface member access"] = [2, 3];
        Assert.Contains("mixes both forms (2 with, 3 without)", Conventions.InterfaceAccess(counts, always, enforcedInBuild: false)?.Report, StringComparison.Ordinal);

        // Enforced in the build (EnforceCodeStyleInBuild): the key stays the team's.
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_style_require_accessibility_modifiers = always:warning\n");
        Write("Directory.Build.props", "<Project><PropertyGroup><EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild></PropertyGroup></Project>\n");
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.DoesNotContain("= for_non_interface_members", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);
    }

    [Fact]
    public void Init_WritesTheSameBlockTwice_WithCSharpierAndAKeyTheCodeContradicts()
    {
        // sharpcompress: a second init moved BRO1520's and BRO1406's lines (kept lines went first, recomputed ones after).
        Write(".config/dotnet-tools.json", "{ \"tools\": { \"csharpier\": { \"version\": \"0.28.0\" } } }\n");
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_style_operator_placement_when_wrapping = end_of_line\ndotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/C.cs", "class C\n{\n    int A(int a, int b) => a + (b * 2);\n\n    int B(int a, int b) => a + b * 2;\n\n    int D(int a, int b) => (a * b) - 1;\n\n    int E(int a, int b) => a * b - 1;\n\n"
            + "    bool F(object a) => a == null;\n\n    bool G(object a) => a is null;\n\n    bool H(object a) => a == null;\n\n    bool J(object a) => a is null;\n\n    bool K(object a) => a is null;\n\n    bool L(object a) => a == null;\n}\n");

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("so BRO1406 is off", first, StringComparison.Ordinal);
        Assert.DoesNotContain("so BRO1520 is off", first, StringComparison.Ordinal);
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains(InitCommand.PresentLine, output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_SaysNothingAboutIde0055Crashes_WhenCSharpierTurnsItOff()
    {
        Write(".csharpierrc", "{}\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFrameworks>net8.0;net10.0</TargetFrameworks></PropertyGroup></Project>\n");
        Write("App/C.cs", "class C\n{\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));

        Assert.DoesNotContain(InitCommand.MultiTargetedHint, output.Replace("\r\n", "\n"), StringComparison.Ordinal);
        File.Delete(Path.Combine(root, ".csharpierrc"));
        Assert.Contains(InitCommand.MultiTargetedHint, Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void PackagesConfig_EntryGetsALineOfItsOwn_AndNuGetsTargetIsFoundHoweverItIsWritten()
    {
        // synth2: a one-line packages.config got the entry on the same line.
        var config = PackagesConfig.AddEntry("<?xml version=\"1.0\" encoding=\"utf-8\"?><packages><package id=\"Newtonsoft.Json\" version=\"13.0.3\" /></packages>\n", "1.0.0", null);
        Assert.Contains("/>\n  <package id=\"StyleBro.Analyzers\" version=\"1.0.0\" developmentDependency=\"true\" />\n</packages>", config, StringComparison.Ordinal);
        Assert.Equal("<packages>\n  <package id=\"StyleBro.Analyzers\" version=\"1.0.0\" developmentDependency=\"true\" />\n</packages>\n", PackagesConfig.AddEntry("<packages>\n</packages>\n", "1.0.0", null));

        // Single quotes and '</Target>' after other content: still NuGet's target, not a second one that replaces it.
        var project = "<Project>\n  <Target Name='EnsureNuGetPackageBuildImports' BeforeTargets='PrepareForBuild'><Error Condition=\"x\" /></Target>\n</Project>\n";
        var result = PackagesConfig.AddToProject(project, @"..\packages\StyleBro.Analyzers.1.0.0");
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(result, "EnsureNuGetPackageBuildImports"));
        Assert.Contains("<Error Condition=\"x\" />\n    <Error Condition=\"!Exists('..\\packages\\StyleBro.Analyzers.1.0.0\\build\\StyleBro.Analyzers.targets')\"", result, StringComparison.Ordinal);
    }
}
