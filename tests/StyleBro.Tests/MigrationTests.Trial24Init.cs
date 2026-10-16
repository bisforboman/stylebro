using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void Init_CSharpier_TurnsTheModifierOrderOff()
    {
        // Trial 24 (Dawnshard): IDE0036 at warning against CSharpier's own modifier order, 'file static class' and 'static file
        // class' back and forth.
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write(".csharpierrc", "printWidth: 100\n");
        Write("Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "namespace Lib;\n\nstatic file class C\n{\n    public static int X() => 1;\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var text = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.True(text.LastIndexOf("dotnet_diagnostic.IDE0036.severity = none", StringComparison.Ordinal) > text.LastIndexOf("dotnet_diagnostic.IDE0036.severity = warning", StringComparison.Ordinal));
        Assert.Contains("IDE0036 too (CSharpier orders modifiers itself)", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Conventions_CompactMembers_TurnBro1519OffWithBro1505()
    {
        // Trial 24 (c1g): BRO1505 off for compact members, and BRO1519 added a blank line after every method's '}' anyway.
        var counts = Conventions.NewCounts();
        counts["dotnet_diagnostic.BRO1505.severity"] = new[] { 0, 16 };

        var (lines, report) = Conventions.Decide(counts, new Dictionary<string, string>());

        Assert.Contains("dotnet_diagnostic.BRO1505.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.BRO1519.severity = none", lines);
        Assert.Contains(report, l => l.Contains("BRO1519 is off too", StringComparison.Ordinal));

        // With blank lines between members both stay; a BRO1519 the team set is theirs.
        counts["dotnet_diagnostic.BRO1505.severity"] = new[] { 16, 0 };
        Assert.DoesNotContain("dotnet_diagnostic.BRO1519.severity = none", Conventions.Decide(counts, new Dictionary<string, string>()).Lines);
        counts["dotnet_diagnostic.BRO1505.severity"] = new[] { 0, 16 };
        Assert.DoesNotContain("dotnet_diagnostic.BRO1519.severity = none", Conventions.Decide(counts, new Dictionary<string, string>(), key => key == "dotnet_diagnostic.BRO1519.severity").Lines);
    }

    [Fact]
    public void Conventions_CountOneLineBlocksWithCode()
    {
        // Trial 24 (c1c2): 'public int Next() { return ++count; }' and 'if (x) { return; }' everywhere, and BRO1509/BRO1508
        // expanded them all. Only one statement that fits a line counts; lambdas and accessors don't.
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            using System;

            public class C
            {
                private int count;

                public int Next() { return ++count; }

                public int Prev()
                {
                    return --count;
                }

                public void Reset()
                {
                    if (count == 0) { return; }
                    while (count > 1) { count--; }
                    if (count == 1)
                    {
                        count = 0;
                    }

                    Action a = () => { count++; };
                    count = 2;
                }

                public int X { get { return count; } }
            }
            """),
            counts);

        Assert.Equal(new[] { 1, 1 }, counts[Conventions.OneLineBodiesKey]);
        Assert.Equal(new[] { 1, 2 }, counts["dotnet_diagnostic.BRO1508.severity"]);
        Assert.Equal(new[] { 0, 1 }, counts["dotnet_diagnostic.BRO1509.severity"]);

        // The method bodies decide BRO1509 when they keep one-line bodies and the properties don't.
        counts["dotnet_diagnostic.BRO1509.severity"] = new[] { 20, 0 };
        counts[Conventions.OneLineBodiesKey] = new[] { 1, 12 };
        Conventions.FoldOneLineBodies(counts);
        Assert.Equal(new[] { 1, 12 }, counts["dotnet_diagnostic.BRO1509.severity"]);
        Assert.False(counts.ContainsKey(Conventions.OneLineBodiesKey));

        // akka.net: one-line properties among many multi-line methods still turn it off.
        counts["dotnet_diagnostic.BRO1509.severity"] = new[] { 0, 271 };
        counts[Conventions.OneLineBodiesKey] = new[] { 3000, 0 };
        Conventions.FoldOneLineBodies(counts);
        Assert.Equal(new[] { 0, 271 }, counts["dotnet_diagnostic.BRO1509.severity"]);
    }

    [Fact]
    public void Init_OneLineBlocks_StayOnOneLine()
    {
        // Trial 24 (c1c2): with BRO1508 off, IDE0055 still split 'if (x) { return; }' unless csharp_preserve_single_line_statements is true.
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        foreach (var name in new[] { "A", "B", "C" })
        {
            Write($"{name}.cs", $"namespace Lib;\n\npublic class {name}\n{{\n    private int count;\n\n    public int Next() {{ return ++count; }}\n\n    public void Touch(bool flag)\n    {{\n        if (flag) {{ count++; }}\n    }}\n}}\n");
        }

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var text = File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains("dotnet_diagnostic.BRO1508.severity = none\n", text, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.BRO1509.severity = none\n", text, StringComparison.Ordinal);
        Assert.Contains("('if (x) { return; }') on one line only with this\ncsharp_preserve_single_line_statements = true\n", text, StringComparison.Ordinal);

        // A second init writes the same block.
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(text, File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n"));
    }

    [Fact]
    public void Conventions_CountTheDocElementOrder()
    {
        // Trial 24 (Spectre.Console): '<remarks>' right below the summary, above params and returns; BRO1619 moved ~40.
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            public class C
            {
                /// <summary>A.</summary>
                /// <remarks>See.</remarks>
                /// <param name="x">X.</param>
                /// <returns>Y.</returns>
                public int A(int x) => x;

                /// <summary>B.</summary>
                /// <param name="x">X.</param>
                /// <returns>Y.</returns>
                /// <exception cref="System.Exception">Never.</exception>
                public int B(int x) => x;

                /// <summary>C.</summary>
                /// <param name="x">X.</param>
                /// <returns>Y.</returns>
                public int D(int x) => x;

                /// <summary>E.</summary>
                /// <remarks>Only remarks.</remarks>
                public int E() => 1;
            }
            """),
            counts);

        Assert.Equal(new[] { 1, 1 }, counts["dotnet_diagnostic.BRO1619.severity"]);
    }

    [Fact]
    public void Conventions_LeaveOptOutFoldersOut()
    {
        // Trial 24 (c1e, b3c): samples/ turns every analyzer off and still voted ("4 of 6 files sorted").
        Write("src/A.cs", "class A { }\n");
        Write("samples/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = none\n");
        Write("samples/S.cs", "class S { }\n");
        Write("vendor2/.editorconfig", "[*.cs]\ngenerated_code = true\n");
        Write("vendor2/V.cs", "class V { }\n");
        Write("tests/.editorconfig", "[*.cs]\ndotnet_diagnostic.BRO1001.severity = none\n");
        Write("tests/T.cs", "class T { }\n");

        var files = Migration.OwnCSharpFiles(root).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')).Order().ToList();

        Assert.Equal(new[] { "src/A.cs", "tests/T.cs" }, files);
    }

    [Fact]
    public void HasParentProps_StopsAtTheRepositoryRoot()
    {
        // Trial 24: a clone below a folder with a Directory.Build.props (the trial's own) got an import of it in its new root props.
        Write("Directory.Build.props", "<Project />\n");
        Directory.CreateDirectory(Path.Combine(root, "clone", ".git"));
        Assert.False(Migration.HasParentProps(Path.Combine(root, "clone", "Directory.Build.props")));

        // Inside the repository it still counts.
        Write("clone/Directory.Build.props", "<Project />\n");
        Assert.True(Migration.HasParentProps(Path.Combine(root, "clone", "src", "Directory.Build.props")));
    }

    [Fact]
    public void Suppressions_ASecondRun_SaysTheyWereCarriedOver()
    {
        var replacements = new Dictionary<string, SortedSet<string>> { ["SA1623"] = new(StringComparer.Ordinal) { "BRO1604" } };
        Write("App/B.cs", "#pragma warning disable SA1623\nclass B { }\n");
        Assert.Equal((1, 1, 0, 0), Program.RewriteSuppressions(root, replacements, write: true));
        Assert.Equal((0, 0, 0, 1), Program.RewriteSuppressions(root, replacements, write: true));
    }

    [Fact]
    public void ThePackageSwap_PutsAGlobalReferenceNextToStyleCops()
    {
        // Trial 24 (Nerdbank.Streams): StyleBro's GlobalPackageReference came in a new ItemGroup at the end.
        const string Packages = "<Project>\n  <ItemGroup>\n    <GlobalPackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" />\n  </ItemGroup>\n  <ItemGroup>\n    <PackageVersion Include=\"X\" Version=\"1.0.0\" />\n  </ItemGroup>\n</Project>\n";
        var result = PreviewCommand.AddNextToStyleCop(Packages, "<GlobalPackageReference Include=\"StyleBro.Analyzers\" Version=\"1.0.0\" />", always: true);
        Assert.Contains("Version=\"1.2.0-beta.556\" />\n    <GlobalPackageReference Include=\"StyleBro.Analyzers\" Version=\"1.0.0\" />\n  </ItemGroup>", result, StringComparison.Ordinal);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(result, "<ItemGroup").Count);
    }

    [Fact]
    public void EditFile_NeverWritesAnEmptyProjectFile()
    {
        var props = Path.Combine(root, "src", "Directory.Build.props");
        Directory.CreateDirectory(Path.GetDirectoryName(props)!);
        PreviewCommand.EditFile(props, _ => " \n");
        Assert.False(File.Exists(props));
    }
}
