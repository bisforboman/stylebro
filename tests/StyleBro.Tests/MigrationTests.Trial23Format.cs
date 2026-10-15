using StyleBro.Analyzers;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Trial 23's findings in 'stylebro-migrate format', verify, the preview and baseline.</summary>
public sealed partial class MigrationTests
{
    [Theory]

    // A string in an '#elif' branch: neither "no symbol" nor "every symbol" compiles it.
    [InlineData("class C\n{\n#if NET10_0_OR_GREATER\n    const string S = \"\";\n#elif NET8_0_OR_GREATER\n    const string S = \"\"\"\n        #pragma warning disable CS0618\n        namespace Y;\n        \"\"\";\n#else\n    const string S = \"\";\n#endif\n}\n", "#pragma")]

    // Under a mixed condition.
    [InlineData("class C\n{\n#if NET8_0_OR_GREATER && !NET10_0_OR_GREATER\n    const string S = \"\"\"\n        #nullable enable\n        class Legacy { }\n        \"\"\";\n#else\n    const string S = \"\";\n#endif\n}\n", "#nullable")]

    // '#if'/'#endif' lines in the string: in the other branch they're nested conditionals the formatter moves.
    [InlineData("class C\n{\n#if NET10_0_OR_GREATER\n    const string S = \"\"\"\n        namespace X;\n        #if DEBUG\n        public partial class A { }\n        #endif\n        \"\"\";\n#else\n    const string S = \"\";\n#endif\n}\n", "#if DEBUG|#endif\n        \"")]
    public void Format_ProtectsStringLinesOfEveryBranch(string text, string lines)
    {
        Assert.Equal(lines.Split('|').Select(l => text.IndexOf(l, StringComparison.Ordinal)).Order(), FormatCommand.DirectiveStringOffsets(text));
    }

    [Fact]
    public void Format_OldMacLineEndings_NotACarriageReturnInAString()
    {
        // jint: a verbatim regex with a CR character; converting the file's line endings would change the regex.
        Write("Pattern.cs", "namespace R;\r\n\r\npublic static class P\r\n{\r\n    public const string Row = @\"^\\| x \\|\r?$\";\r\n    public const string Raw = \"\"\"\r\n        a\rb\r\n        \"\"\";\r\n}\r\n");
        Write("Mac.cs", "class A\r{\r}\r");
        Write("Comment.cs", "class B\r\n{\r\n    // a comment\r    int x;\r\n}\r\n");

        Assert.Equal(["Comment.cs", "Mac.cs"], FormatCommand.OldMacLineEndings(root));
    }

    [Fact]
    public void Format_LeavesVisualBasicAlone()
    {
        Write("App.slnx", "<Solution>\n  <Project Path=\"src/Billing/Billing.csproj\" />\n  <Project Path=\"src/Billing.Legacy/Billing.Legacy.vbproj\" />\n</Solution>\n");
        Write("App.sln", "Project(\"{F184B08F-C81C-45F6-A57F-5ABD9991F28F}\") = \"Old\", \"src\\Old\\Old.vbproj\", \"{1}\"\nEndProject\n");
        Assert.Equal(["Billing.Legacy.vbproj"], FormatCommand.VisualBasicProjects(Path.Combine(root, "App.slnx"), []));
        Assert.Equal(["Old.vbproj", "Ref.vbproj"], FormatCommand.VisualBasicProjects(Path.Combine(root, "App.sln"), [Path.Combine(root, "src", "Ref", "Ref.vbproj"), Path.Combine(root, "src", "A", "A.csproj")]));
        Assert.Empty(FormatCommand.VisualBasicProjects(Path.Combine(root, "src", "A", "A.csproj"), []));

        // Format doesn't load them: a C# member a .vb file uses keeps its name (the renamers check these names).
        Write("src/Billing.Legacy/LegacyInvoice.vb", "Public Class LegacyInvoice\n    Function Total() As Decimal\n        Return Billing.Tariff.base_rate\n    End Function\nEnd Class\n");
        Write("src/Billing/Tariff.cs", "namespace Billing;\npublic class Tariff { }\n");
        var names = Path.Combine(root, "names.tsv");
        FormatCommand.WriteRepositoryNames(root, names);
        var visualBasic = Assert.Single(File.ReadAllLines(names).Select(RepositoryNames.Parse).OfType<RepositoryNames.Entry>(), e => e.Where.Contains(".vb (Visual Basic", StringComparison.Ordinal));
        Assert.Equal(RepositoryNames.IdentifierKind, visualBasic.Kind);
        Assert.Contains("base_rate", visualBasic.Text.Split(' '));

        // A .vb file a fix still reaches is put back.
        var log = new List<string>();
        var file = Path.Combine(root, "src", "Billing.Legacy", "LegacyInvoice.vb");
        var before = File.ReadAllText(file);
        using (new FormatCommand.UntouchedFiles(root, ".vb", log.Add))
        {
            File.WriteAllText(file, before.Replace("base_rate", "BaseRate", StringComparison.Ordinal));
        }

        Assert.Equal(before, File.ReadAllText(file));
        Assert.StartsWith("Put back: " + file, Assert.Single(log), StringComparison.Ordinal);
    }

    [Fact]
    public void Format_BaselinesTheFindingsItKeeps()
    {
        // jint: 227 kept findings, then 369 errors in a build with warnings as errors. Owner's choice: baseline them.
        Write("src/C.cs", "class C\n{\n    private int Count;\n    private int Total;\n}\n");
        Write("src/Old.cs", "class Old\n{\n}\n");
        var kept = new[]
        {
            new KeptFinding(Path.Combine(root, "src", "C.cs"), 3, 17, "BRO1303", KeptReason.NameInString, "Rename 'Count' to 'count'"),
            new KeptFinding(Path.Combine(root, "src", "C.cs"), 4, 17, "BRO1303", KeptReason.NameInString, "Rename 'Total' to 'total'"),
        };
        var line = FormatCommand.BaselineKept(root, kept);
        Assert.StartsWith("Baseline: stylebro.baseline: added 2 kept findings, so the build and 'format --verify-no-changes' skip them", line, StringComparison.Ordinal);
        var file = Path.Combine(root, "stylebro.baseline");
        var entries = Analyzers.Baseline.Baseline.Parse(File.ReadAllText(file)).Entries;
        Assert.Equal(2, entries.Count);
        Assert.Contains(new Analyzers.Baseline.Baseline.Key("BRO1303", "src/C.cs", Analyzers.Baseline.Baseline.Fingerprint("private int Count;")), entries.Keys);

        // The same again (the build suppresses them now, so format no longer keeps them): nothing to write.
        Assert.Null(FormatCommand.BaselineKept(root, kept));
        Assert.Null(FormatCommand.BaselineKept(root, []));

        // Another tool's entries stay while their line is there; the ones whose line is gone go.
        File.AppendAllText(file, "BRO1505\tsrc/Old.cs\t" + Analyzers.Baseline.Baseline.Fingerprint("class Old") + "\t1\nBRO1505\tsrc/Gone.cs\t0000000000000000\t2\n");
        Write("src/C.cs", "class C\n{\n    private int Count;\n    private int Sum;\n}\n");
        Assert.Equal("Baseline: stylebro.baseline: removed 3 entries whose line is gone (https://bisforboman.github.io/stylebro/baseline/).", FormatCommand.BaselineKept(root, []));
        Assert.Equal(["src/C.cs", "src/Old.cs"], Analyzers.Baseline.Baseline.Parse(File.ReadAllText(file)).Entries.Keys.Select(k => k.Path).Order(StringComparer.Ordinal));

        // The props name another file, or turn it off.
        Write("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <StyleBroBaseline>$(MSBuildThisFileDirectory)build/stylebro.baseline</StyleBroBaseline>\n  </PropertyGroup>\n</Project>\n");
        Assert.Equal(Path.Combine(root, "build", "stylebro.baseline"), FormatCommand.BaselineFile(Path.Combine(root, "src")));
        Write("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <StyleBroBaseline>none</StyleBroBaseline>\n  </PropertyGroup>\n</Project>\n");
        Assert.Null(FormatCommand.BaselineKept(root, kept));
    }

    [Fact]
    public void Format_CleanRunWithKeptFindings_WritesTheBaseline_AndSaysSo()
    {
        Write("C.cs", "class C\n{\n    private int Count;\n}\n");
        var lines = new List<string>();
        var code = FormatCommand.Run([root], lines.Add, (args, _) =>
        {
            File.AppendAllText(Environment.GetEnvironmentVariable(KeptFinding.Variable)!, new KeptFinding(Path.Combine(root, "C.cs"), 3, 17, "BRO1303", KeptReason.NameInString, "Rename 'Count' to 'count'") + "\n");
            return 0;
        });

        Assert.Equal(0, code);
        Assert.Contains(lines, l => l.StartsWith("Baseline: stylebro.baseline: added 1 kept finding,", StringComparison.Ordinal));
        Assert.Single(Analyzers.Baseline.Baseline.Parse(File.ReadAllText(Path.Combine(root, "stylebro.baseline"))).Entries);
    }

    [Fact]
    public void Format_AFixThatFailed_IsReportedToWhoeverShipsIt()
    {
        Assert.EndsWith("Please report it at https://github.com/bisforboman/stylebro/issues with this output.", FormatCommand.FixFailuresLine(["BRO1309 (CamelCaseNamingCodeFixProvider): boom"]), StringComparison.Ordinal);
        var other = FormatCommand.FixFailuresLine(["XX0001 (BadFix): fix boom"]);
        Assert.DoesNotContain("bisforboman", other, StringComparison.Ordinal);
        Assert.Contains("XX0001 is not StyleBro's: report it to the analyzer package or project that has that rule", other, StringComparison.Ordinal);
        Assert.Contains("IDE0055 is the .NET SDK's: https://github.com/dotnet/roslyn/issues", FormatCommand.FixFailuresLine(["IDE0055 (Fixer): x", "BRO1001: y"]), StringComparison.Ordinal);
    }

    [Fact]
    public void Format_ExplainsASkippedProject_WithoutBlamingTheRestore()
    {
        // FluentAssertions: after a restore that worked, "Run `dotnet restore`": the net47 run had loaded a netstandard2.0
        // project for net47, whose reference assemblies weren't there.
        FormatCommand.ResetExplained();
        var line = FormatCommand.Explain("Required references did not load for Specs or referenced project. Run `dotnet restore` prior to formatting.");
        Assert.StartsWith("'dotnet format' skipped Specs: MSBuild couldn't load it or a project it references for this run", line, StringComparison.Ordinal);
        Assert.Contains("The restore isn't the cause", line, StringComparison.Ordinal);
    }

    [Fact]
    public void StaleCopies_OlderThanADay_AreDeleted()
    {
        var old = Directory.CreateDirectory(Path.Combine(root, "stylebro-preview-old")).FullName;
        var verify = Directory.CreateDirectory(Path.Combine(root, "stylebro-verify-old")).FullName;
        var fresh = Directory.CreateDirectory(Path.Combine(root, "stylebro-preview-new")).FullName;
        var other = Directory.CreateDirectory(Path.Combine(root, "other-old")).FullName;
        File.WriteAllText(Path.Combine(old, "a.txt"), "x");
        File.SetAttributes(Path.Combine(old, "a.txt"), FileAttributes.ReadOnly);
        foreach (var folder in new[] { old, verify, other })
        {
            Directory.SetLastWriteTimeUtc(folder, DateTime.UtcNow.AddDays(-2));
        }

        PreviewCommand.DeleteStaleCopies(root, TimeSpan.FromDays(1));

        Assert.False(Directory.Exists(old));
        Assert.False(Directory.Exists(verify));
        Assert.True(Directory.Exists(fresh));
        Assert.True(Directory.Exists(other));
    }

    [Fact]
    public void LaterFrameworks_GetOnlyTheFilesThatCompileDifferently()
    {
        // jint: the core project, fixed in every framework's run (5 times).
        Write("Lib/Plain.cs", "class Plain { }\n");
        Write("Lib/Modern.cs", "class Modern\n{\n#if NET8_0_OR_GREATER\n#endif\n}\n");
        Write("Lib/Debug.cs", "class Debug\n{\n#if DEBUG\n#endif\n}\n");
        Write("Lib/Only8.cs", "class Only8 { }\n");
        Write("Lib/obj/Debug/net8.0/Lib.AssemblyInfo.cs", "// <auto-generated/>\n");
        string F(string name) => Path.Combine(root, "Lib", name);
        var net10 = new FormatCommand.CompileView(["NET8_0_OR_GREATER", "DEBUG"], "14.0", "enable", [F("Plain.cs"), F("Modern.cs"), F("Debug.cs")]);
        var net8 = new FormatCommand.CompileView(["NET8_0_OR_GREATER", "DEBUG"], "14.0", "enable", [F("Plain.cs"), F("Modern.cs"), F("Debug.cs"), F("Only8.cs"), Path.Combine(root, "Lib", "obj", "Debug", "net8.0", "Lib.AssemblyInfo.cs")]);
        var net472 = new FormatCommand.CompileView(["DEBUG"], "14.0", "enable", [F("Plain.cs"), F("Modern.cs"), F("Debug.cs")]);
        var older = new FormatCommand.CompileView(["DEBUG"], "7.3", "enable", [F("Plain.cs")]);

        Assert.Equal(["Lib/Only8.cs"], FormatCommand.FilesNewToFramework(Path.Combine(root, "Lib"), root, net8, [net10]));
        Assert.Equal(["Lib/Modern.cs"], FormatCommand.FilesNewToFramework(Path.Combine(root, "Lib"), root, net472, [net10, net8]));
        Assert.Equal(["Lib/Plain.cs"], FormatCommand.FilesNewToFramework(Path.Combine(root, "Lib"), root, older, [net10]));
    }

    [Fact]
    public void Format_ADotnetFormatThatOutlivesAKilledRun_KeepsItsFilesSwapped_AndStopsTheNextRun()
    {
        // Trial 23: the next command put the '#' back while the killed run's 'dotnet format' still ran, which then wrote the
        // swapped text again: no record, a "damaged" file.
        var records = Path.Combine(root, ".records");
        Write("src/Gen/C.cs", DirectiveString);
        using (var mask = new FormatCommand.DirectiveMask(root, ["src/Gen/C.cs"], records))
        {
            using var current = System.Diagnostics.Process.GetCurrentProcess();
            mask.Track(current);
            var record = Assert.Single(Directory.GetFiles(records));

            // The run is gone (another process has its id now), its 'dotnet format' (this process) still runs.
            File.WriteAllText(record, new System.Text.RegularExpressions.Regex("\"Pid\":\\d+,\"Started\":\\d+").Replace(File.ReadAllText(record), "\"Pid\":1,\"Started\":1", 1));
            var log = new List<string>();
            FormatCommand.DirectiveMask.Recover(log.Add, records);
            Assert.Empty(log);
            Assert.DoesNotContain("#pragma", File.ReadAllText(Path.Combine(root, "src", "Gen", "C.cs")), StringComparison.Ordinal);
            Assert.Equal(["src/Gen/C.cs"], FormatCommand.DirectiveMask.Busy(root, records));
        }

        // A filter a killed run left next to the solution goes; one this process wrote stays.
        Write(".stylebro-format-0.slnf", "{}");
        Write(".stylebro-format-1.slnf", "{}");
        File.SetLastWriteTimeUtc(Path.Combine(root, ".stylebro-format-0.slnf"), DateTime.UtcNow.AddDays(-1));
        FormatCommand.DeleteLeftoverFilters(root);
        Assert.False(File.Exists(Path.Combine(root, ".stylebro-format-0.slnf")));
        Assert.True(File.Exists(Path.Combine(root, ".stylebro-format-1.slnf")));
    }

    [Fact]
    public void Separate_ProjectsSharingAFile_ShareARun_WhenTheSymbolsItTestsAgree()
    {
        // jint: test projects link a helper with other DefineConstants that the helper never tests: 4 runs for net10.0.
        Write("Shared.cs", "class Shared { }\n");
        Write("Tested.cs", "class Tested\n{\n#if EXTRA\n#endif\n}\n");
        var shared = Path.Combine(root, "Shared.cs");
        var tested = Path.Combine(root, "Tested.cs");
        FormatCommand.CompileView View(params string[] defines) => new([.. defines], "14.0", "enable", []);
        var views = new Dictionary<string, FormatCommand.CompileView> { ["A"] = View("NET", "A_ONLY"), ["B"] = View("NET", "B_ONLY"), ["C"] = View("NET", "EXTRA") };
        var files = new Dictionary<string, string[]> { ["A"] = [shared, tested], ["B"] = [shared, tested], ["C"] = [tested] };

        var groups = FormatCommand.Separate(["A", "B", "C"], _ => [], p => files[p], (p, f) => views[p].Key([f]));

        Assert.Equal([["A", "B"], ["C"]], groups);
    }
}
