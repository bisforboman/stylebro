using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What a trial of main on vs-validation, SmartEnum, Scrutor and TodoApi found in format, the preview and --write.</summary>
public sealed partial class MigrationTests
{
    [Theory]
    [InlineData("net6", "net6.0")]
    [InlineData("net60", "net6.0")]
    [InlineData("NET8", "net8.0")]
    [InlineData("net8.0", "net8.0")]
    [InlineData("net6-windows", "net6.0-windows")]
    [InlineData("netcoreapp31", "netcoreapp3.1")]
    [InlineData("netstandard20", "netstandard2.0")]
    [InlineData("net48", "net48")]       // .NET Framework: no dot
    [InlineData("net472", "net472")]
    [InlineData("net10", "net10")]       // NuGet: .NET Framework 1.0, not .NET 10
    [InlineData("net10.0", "net10.0")]
    public void Format_NormalizesFrameworkSpellings_LikeNuGet(string framework, string expected)
    {
        Assert.Equal(expected, FormatCommand.NormalizeFramework(framework));
    }

    [Fact]
    public void Format_SpellingsOfOneFramework_ShareARun_LikeSmartEnum()
    {
        // SmartEnum: EFCore writes 'net6;net7;net8', the rest 'net6.0;...': 7 runs before, 3 now.
        var plan = FormatCommand.Plan(new Dictionary<string, string[]>
        {
            ["EFCore.csproj"] = new[] { "net6", "net7", "net8" },
            ["SmartEnum.csproj"] = new[] { "net6.0", "net7.0", "net8.0", "netstandard2.0" },
            ["EFCore.Tests.csproj"] = new[] { "net7.0", "net6.0", "net8.0" },
        });

        Assert.Equal(new[] { "net6.0", "net7.0", "net8.0", "netstandard2.0" }, plan.Select(p => p.Framework));
        Assert.Equal(new[] { "EFCore.csproj", "EFCore.Tests.csproj", "SmartEnum.csproj" }, plan[0].Projects);
    }

    [Fact]
    public void Format_AProjectThatSpellsTheFrameworkAnotherWay_GetsItsOwnSpelling()
    {
        // The 'net6' run set TargetFramework=net6 for the EFCore tests too, which only have restore output for 'net6.0':
        // "Warnings were encountered while loading the workspace".
        var projects = new Dictionary<string, (string[] Frameworks, string[] References)>
        {
            [@"C:\r\EFCore.csproj"] = (new[] { "net6", "net7" }, Array.Empty<string>()),
            [@"C:\r\EFCore.Tests.csproj"] = (new[] { "net6.0", "net7.0" }, new[] { @"C:\r\EFCore.csproj" }),
            [@"C:\r\Other.Tests.csproj"] = (new[] { "net6.0" }, Array.Empty<string>()),
        };
        var keep = FormatCommand.Keep(projects, "net6.0");

        var (alias, aliases) = FormatCommand.Aliases(projects, keep, "net6.0");

        Assert.Equal(3, keep.Count);
        Assert.Equal("net6.0", alias); // most projects' spelling: their own conditions see it
        Assert.Equal(@"|C:\R\EFCORE.CSPROJ=net6|", aliases);
        Assert.Contains("StyleBroFormatAliases", FormatCommand.SelectFrameworkTargets, StringComparison.Ordinal);
        Assert.Equal(string.Empty, FormatCommand.Aliases(projects, new[] { @"C:\r\Other.Tests.csproj" }, "net6.0").Aliases);
    }

    [Fact]
    public void Format_WritesTheNamesOfEveryCSharpFile_ForRunsThatLoadOnlySomeProjects()
    {
        // Scrutor: the tests read 'count' with GetField; the netstandard2.0 run loads only the library.
        Write("src/Lib/Counter.cs", "class Counter\n{\n    private int count;\n}\n");
        Write("test/Tests/CounterTests.cs", "class CounterTests\n{\n    object Read(object c) => c.GetType().GetField(\"count\", 0)!;\n\n    string Name => nameof(Counter);\n}\n");
        Write("src/Lib/obj/Generated.cs", "class Generated { string S => \"not this\"; }\n");
        var file = Path.Combine(root, "names.tsv");

        FormatCommand.WriteRepositoryNames(root, file);

        var entries = File.ReadAllLines(file).Select(StyleBro.Analyzers.RepositoryNames.Parse).OfType<StyleBro.Analyzers.RepositoryNames.Entry>().ToList();
        var tests = Path.Combine(root, "test", "Tests", "CounterTests.cs");
        Assert.Contains(entries, e => e.Kind == 'S' && e.Text == "count" && e.Where == tests + "(3)");
        Assert.Contains(entries, e => e.Kind == 'N' && e.Text == "Counter");
        Assert.Contains(entries, e => e.Kind == 'I' && e.Where == tests && e.Text.Split(' ').Contains("GetField"));
        Assert.DoesNotContain(entries, e => e.Text == "not this"); // bin and obj aren't sources
    }

    [Fact]
    public void RepositoryNames_KeepStringsWithTabsAndLineBreaks()
    {
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("class C { string S => \"a\\tb\\nc\\\\d\"; }");

        var entry = StyleBro.Analyzers.RepositoryNames.Parse(StyleBro.Analyzers.RepositoryNames.Lines(tree.GetRoot(), "C:\\r\\C.cs").First())!;

        Assert.Equal("a\tb\nc\\d", entry.Text);
        Assert.Equal("C:\\r\\C.cs(1)", entry.Where);
        Assert.Null(StyleBro.Analyzers.RepositoryNames.Parse("not a line"));
    }

    [Fact]
    public void Preview_AGlobalPackageReferenceInANestedPackagesProps_GetsAGlobalPackageReference_LikeVsValidation()
    {
        // StyleCop's GlobalPackageReference moved into src/Directory.Packages.props, which imports the root's. The preview
        // wrote a PackageReference with a version there: NU1008, and the package twice.
        Write("Directory.Packages.props", "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>\n");
        Write("src/Directory.Packages.props", "<Project>\n  <Import Project=\"$([MSBuild]::GetPathOfFileAbove('Directory.Packages.props', '$(MSBuildThisFileDirectory)../'))\" />\n  <ItemGroup>\n    <GlobalPackageReference Include=\"StyleCop.Analyzers.Unstable\" Version=\"1.2.0.556\" />\n  </ItemGroup>\n</Project>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");

        var added = PreviewCommand.AddPackage(root);

        var text = File.ReadAllText(Path.Combine(root, "src", "Directory.Packages.props"));
        Assert.Matches("<GlobalPackageReference Include=\"StyleBro.Analyzers\" Version=\"[^\"]+\" />", text);
        Assert.DoesNotContain("<PackageReference", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StyleBro", File.ReadAllText(Path.Combine(root, "Directory.Packages.props")), StringComparison.Ordinal);
        Assert.Contains("src/Directory.Packages.props", added, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_CentralPackageManagement_IsFoundThroughImports()
    {
        // A project's nearest Directory.Packages.props doesn't turn it on itself; the root's it imports does.
        Write("Directory.Packages.props", "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>\n");
        Write("src/Directory.Packages.props", "<Project>\n  <Import Project=\"../Directory.Packages.props\" />\n</Project>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");

        Assert.Equal(Path.Combine(root, "src", "Directory.Packages.props"), PreviewCommand.CentralPackages(Path.Combine(root, "src", "Lib", "Lib.csproj"), root));
        Write("src/Directory.Packages.props", "<Project />\n");
        Assert.Null(PreviewCommand.CentralPackages(Path.Combine(root, "src", "Lib", "Lib.csproj"), root));
    }

    [Fact]
    public void Write_TurnsThePresetOff_OnlyWhereStyleCopRuns_LikeVsValidation()
    {
        // vs-validation: StyleCop runs in src only; test/Directory.Build.props got the opt-out, though no settings apply there.
        Write("src/Directory.Build.props", "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" />\n  </ItemGroup>\n</Project>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("test/Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <IsPackable>false</IsPackable>\n  </PropertyGroup>\n</Project>\n");
        Write("test/Tests/Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");

        var output = Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write" })));

        Assert.Contains("<StyleBroPreset>none</StyleBroPreset>", File.ReadAllText(Path.Combine(root, "src", "Directory.Build.props")), StringComparison.Ordinal);
        Assert.DoesNotContain("StyleBroPreset", File.ReadAllText(Path.Combine(root, "test", "Directory.Build.props")), StringComparison.Ordinal);
        Assert.DoesNotContain("test/Directory.Build.props", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_NamesPropsFilesAsPropsFiles()
    {
        Assert.Equal(
            "1 project targets several frameworks (src/Lib.csproj); 2 props files set several for other projects (Directory.Build.props, test/Directory.Build.props).",
            InitCommand.MultiTargetedLine(new[] { "src/Lib.csproj", "Directory.Build.props", "test/Directory.Build.props" }));
        Assert.Equal("1 props file sets several for the projects (Directory.Build.props).", InitCommand.MultiTargetedLine(new[] { "Directory.Build.props" }));
    }

    [Fact]
    public void Format_RestoreHint_NamesTheNuGetError()
    {
        var output = new[]
        {
            "  Determining projects to restore...",
            @"C:\r\src\Lib\Lib.csproj : error NU1008: The following PackageReference items cannot define a value for Version: StyleBro.Analyzers. Projects using Central Package Management must define a Version value on a PackageVersion item. For more information, visit https://aka.ms/nuget/cpm/gettingstarted [C:\r\App.slnx]",
        };

        var hint = FormatCommand.RestoreHintFor(output);

        Assert.StartsWith("The restore failed with NU1008: The following PackageReference items cannot define a value for Version: StyleBro.Analyzers.", hint, StringComparison.Ordinal);
        Assert.DoesNotContain("audit", hint, StringComparison.Ordinal);
        Assert.Contains("nu1008", hint, StringComparison.Ordinal);
        Assert.Equal(FormatCommand.RestoreHint, FormatCommand.RestoreHintFor(new[] { "Restore operation failed" }));
    }

    [Fact]
    public void Format_VerifyNoChanges_SaysItChangesNothing()
    {
        Assert.StartsWith("Checking StyleBro's rules", FormatCommand.RulesLine(verify: true, 140), StringComparison.Ordinal);
        Assert.Contains("changing nothing", FormatCommand.RulesLine(verify: true, 140), StringComparison.Ordinal);
        Assert.StartsWith("Fixing StyleBro's rules", FormatCommand.RulesLine(verify: false, 140), StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_AFailedFormat_SaysWhyFirst_AndDoesntRepeatTheInitReport()
    {
        Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "class C { }\n");
        PreviewCommand.MakeRepository(root);
        var patch = Path.Combine(root, "out.patch");
        var error = string.Empty;

        var output = Capture(() => error = CaptureError(() => Assert.Equal(1, PreviewCommand.Run("init", new[] { root, "--diff=" + patch, "--no-restore" }, (args, log) =>
        {
            Assert.DoesNotContain("--no-restore", args); // the preview added the package: the copy needs a restore
            log("error NU1101: Unable to find package StyleBro.Analyzers.");
            return 1;
        }))));

        Assert.StartsWith("The preview failed: 'stylebro-migrate format' failed (exit code 1).", error.Replace("\r\n", "\n"), StringComparison.Ordinal);
        Assert.Contains("  error NU1101", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Wrote the built-in rule severities", error + output, StringComparison.Ordinal);
        Assert.Contains("--no-restore: ignored", output, StringComparison.Ordinal);
        var kept = System.Text.RegularExpressions.Regex.Match(output, @"The copy is kept: (.*) \(log").Groups[1].Value;
        DeleteFolder(Path.GetDirectoryName(kept)!);
    }

    [Fact]
    public void Preview_AcceptsNoRestore()
    {
        Assert.Contains("--no-restore", PreviewCommand.Options);
    }
}
