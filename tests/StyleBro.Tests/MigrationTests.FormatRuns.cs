using StyleBro.Analyzers;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>How 'stylebro-migrate format' plans its runs on a large multi-targeted repository (trial 21, splat: 150 runs before).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Format_ProjectsSharingAFileWithTheSameSettings_ShareARun()
    {
        // splat: every library links Polyfills/ArgumentGuard.cs and references Splat.Core, which compiles it too; test
        // projects link Splat.Tests' base classes. Same symbols everywhere, so one run (it was a run per project).
        var compile = new Dictionary<string, string[]>
        {
            ["Core"] = new[] { "Guard.cs", "core.cs" },
            ["A"] = new[] { "Guard.cs", "a.cs" },
            ["B"] = new[] { "Guard.cs", "b.cs" },
            ["A.Tests"] = new[] { "Base.cs", "at.cs" },
            ["B.Tests"] = new[] { "Base.cs", "bt.cs" },
            ["Aot"] = new[] { "Guard.cs", "aot.cs" },
        };
        var references = new Dictionary<string, string[]>
        {
            ["A"] = new[] { "Core" },
            ["B"] = new[] { "Core" },
            ["A.Tests"] = new[] { "A" },
            ["B.Tests"] = new[] { "B" },
        };
        IEnumerable<string> Refs(string p) => references.TryGetValue(p, out var r) ? r : Array.Empty<string>();
        var settings = new Dictionary<string, string?> { ["Core"] = "TRACE", ["A"] = "TRACE", ["B"] = "TRACE", ["A.Tests"] = "TRACE", ["B.Tests"] = "TRACE", ["Aot"] = "TRACE;AOT" };

        var groups = FormatCommand.Separate(compile.Keys, Refs, p => compile[p], p => settings[p]);

        // The project that parses the shared file with other symbols still runs apart (kubernetes-client's Aot project).
        Assert.Equal(new List<List<string>> { new() { "Core", "A", "B", "A.Tests", "B.Tests" }, new() { "Aot" } }, groups);

        // Unknown settings (a project that doesn't target the run's framework, an evaluation that failed): apart, as before.
        settings["B"] = null;
        Assert.Equal(new List<List<string>> { new() { "Core", "A", "A.Tests" }, new() { "B", "B.Tests" }, new() { "Aot" } }, FormatCommand.Separate(compile.Keys, Refs, p => compile[p], p => settings[p]));
        Assert.Equal(4, FormatCommand.Separate(compile.Keys, Refs, p => compile[p]).Count); // without settings: as before
    }

    [Fact]
    public void Format_ARunWhoseFilesTheWhitespaceExclusionsCover_HasNoWhitespacePass()
    {
        // splat: CSharpier's settings turn IDE0055 off in every project folder but a file outside them, so each of the 150
        // runs made an empty whitespace pass.
        var exclusions = new[] { "Polyfills/", "Splat.Core/", "tests/" };
        Assert.Equal(new[] { "style", "analyzers" }, FormatCommand.Passes(exclusions, new[] { "Splat.Core/", "Polyfills/", "tests/Splat.Tests/" }));

        // None of them in the run: one plain run of all three; some: the whitespace pass on its own, as before.
        Assert.Equal(new[] { string.Empty }, FormatCommand.Passes(exclusions, new[] { "Splat.Drawing/" }));
        Assert.Equal(new[] { "whitespace", "style", "analyzers" }, FormatCommand.Passes(exclusions, new[] { "Splat.Core/", "Splat.Drawing/" }));
        Assert.Equal(new[] { "whitespace", "style", "analyzers" }, FormatCommand.Passes(exclusions, new[] { "**/*.cs" }));
        Assert.Equal(new[] { "whitespace", "style", "analyzers" }, FormatCommand.Passes(new[] { "src/A/Gen/" }, new[] { "src/A/" }));
        Assert.Equal(new[] { string.Empty }, FormatCommand.Passes(null, new[] { "Splat.Core/" }));
        Assert.Equal(new[] { "style", "analyzers" }, FormatCommand.Passes(Array.Empty<string>(), new[] { "Splat.Core/" }));
        Assert.Equal(new[] { "style", "analyzers" }, FormatCommand.Passes(new[] { "src/A/B.cs" }, new[] { "src/A/B.cs" }));
    }

    [Fact]
    public void Format_NamedFiles_RunOnlyWhereTheirProjectsAre()
    {
        Assert.Equal(new[] { "src/A/" }, FormatCommand.RunInclude(null, new[] { "src/A/" }));
        Assert.Equal(new[] { "src/A/X.cs" }, FormatCommand.RunInclude(new[] { "src/A/X.cs", "src/B/Y.cs" }, new[] { "src/A/", "Shared/G.cs" }));
        Assert.Equal(new[] { "Shared/G.cs" }, FormatCommand.RunInclude(new[] { "Shared/G.cs" }, new[] { "src/A/", "Shared/G.cs" }));
        Assert.Empty(FormatCommand.RunInclude(new[] { "src/B/Y.cs" }, new[] { "src/A/" }));
        Assert.Equal(new[] { "src/B/" }, FormatCommand.RunInclude(new[] { "src/B/" }, new[] { "src/A/" })); // folders and patterns as named
        Assert.Equal(new[] { "a", "b" }, FormatCommand.OptionValues(new[] { "x", "--include", "a", "b", "--no-restore" }, "--include"));
        Assert.Null(FormatCommand.OptionValues(new[] { "x" }, "--include"));
    }

    [Fact]
    public void Format_ACheckWithOnlyTheKeptFindings_IsClean()
    {
        // splat: 9 findings the fixes keep made the check after the first run fail, and a second full run changed nothing.
        var file = Path.Combine(root, "Splat.Logging", "DefaultLogManager.cs");
        var kept = new List<KeptFinding> { new(file, 17, 41, "BRO1306", KeptReason.NewNameMeansSomethingElse, "Rename '_nullLogger' to 'NullLogger'") };
        var keptLine = $@"{file}(19,41): warning BRO1306: Rename '_nullLogger' to 'NullLogger' [{Path.Combine(root, "Splat.Logging", "Splat.Logging.csproj")}]";
        var other = $@"{file}(30,5): warning BRO1504: Add a blank line before the comment [{Path.Combine(root, "Splat.Logging", "Splat.Logging.csproj")}]";
        IReadOnlyList<string> onlyKept = new[] { keptLine };

        Assert.True(FormatCommand.IsCleanCheck(new[] { (0, (IReadOnlyList<string>)Array.Empty<string>()), (FormatCommand.NotCleanExitCode, onlyKept) }, kept));
        Assert.False(FormatCommand.IsCleanCheck(new[] { (FormatCommand.NotCleanExitCode, onlyKept) })); // nothing known kept
        Assert.False(FormatCommand.IsCleanCheck(new[] { (FormatCommand.NotCleanExitCode, (IReadOnlyList<string>)new[] { keptLine, other }) }, kept));
        Assert.False(FormatCommand.IsCleanCheck(new[] { (FormatCommand.NotCleanExitCode, (IReadOnlyList<string>)Array.Empty<string>()) }, kept));
        Assert.False(FormatCommand.IsCleanCheck(new[] { (1, onlyKept) }, kept)); // an error, not "would change"

        // Listed at its line now (an earlier fix of that run moved it), with the reason the fix gave.
        var now = FormatCommand.AsKept(keptLine, kept);
        Assert.Equal((19, 41, KeptReason.NewNameMeansSomethingElse), (now!.Line, now.Column, now.Reason));
        Assert.Null(FormatCommand.AsKept(keptLine.Replace("NullLogger'", "Other'", StringComparison.Ordinal), kept)); // another rename
        Assert.Null(FormatCommand.AsKept(other, kept));
        Assert.NotNull(FormatCommand.AsKept($"{file}(19,41): error BRO1306: Rename '_nullLogger' to 'NullLogger' (https://bisforboman.github.io/stylebro/rules/BRO1306/) [x.csproj]", kept));

        FormatCommand.Remember(root + Path.DirectorySeparatorChar, kept);
        Assert.Same(kept, FormatCommand.Remembered(root));
        Assert.Empty(FormatCommand.Remembered(Path.Combine(root, "other")));
    }

    [Fact]
    public void Verify_KnowsTheFindingsTheLastCleanRunKept_OnTheSameState()
    {
        // splat: --verify-no-changes after a clean 'format' formatted a copy for 41 minutes to find the 9 kept findings again.
        Write("src/A/A.csproj", "<Project />");
        Write("src/A/X.cs", "class X { }");
        var kept = new[] { new KeptFinding(Path.Combine(root, "src", "A", "X.cs"), 1, 7, "BRO1306", KeptReason.NameInString, "Rename 'a' to 'A'") };
        string[] formatArgs = [root, "--no-restore", FormatCommand.CheckFirstOption];

        FormatCommand.SaveKept(root, formatArgs, kept);

        Assert.Equal(kept.Select(k => k.ToString()), FormatCommand.LoadKept(root, [root, "--verify-no-changes"])!.Select(k => k.ToString()));
        Assert.Null(FormatCommand.LoadKept(root, [root, "--verify-no-changes", "--all"])); // other options
        Write("src/A/.editorconfig", "[*.cs]\nstylebro_rename_public_api = true\n");
        Assert.Null(FormatCommand.LoadKept(root, [root, "--verify-no-changes"])); // other settings
        FormatCommand.SaveKept(root, formatArgs, kept);
        Write("src/A/X.cs", "class X { int a; }");
        Assert.Null(FormatCommand.LoadKept(root, [root, "--verify-no-changes"])); // other code
    }

    [Fact]
    public void Verify_FormatsOnlyTheFilesWithFindings_InTheCopy()
    {
        // splat: the copy was formatted whole (41 minutes) for 9 kept findings in 4 files.
        var lines = new[]
        {
            "== net8.0 (3 project(s))",
            $@"{Path.Combine(root, "src", "A", "X.cs")}(1,2): warning BRO1306: Rename 'a' to 'A' [{Path.Combine(root, "src", "A", "A.csproj")}]",
            $@"{Path.Combine(root, "src", "A", "X.cs")}(5,2): warning BRO1306: Rename 'b' to 'B' [{Path.Combine(root, "src", "A", "A.csproj")}]",
            $@"{Path.Combine(root, "tests", "T.cs")}(3,1): error WHITESPACE: Fix whitespace formatting. [{Path.Combine(root, "tests", "T.csproj")}]",
        };

        Assert.Equal(new[] { "src/A/X.cs", "tests/T.cs" }, FormatCommand.FindingFiles(lines, root));
        Assert.Null(FormatCommand.FindingFiles(lines.Take(1), root));

        // Only their rules, and only the passes that fix them: no style pass for StyleBro's rules.
        Assert.Equal(new[] { "BRO1306" }, FormatCommand.FindingIds(lines));
        Assert.Equal(new[] { "analyzers" }, FormatCommand.Relevant(["style", "analyzers"], ["BRO1306"]));
        Assert.Equal(new[] { "whitespace", "style" }, FormatCommand.Relevant(["whitespace", "style", "analyzers"], ["IDE0005"]));
        Assert.Equal(new[] { "style", "analyzers" }, FormatCommand.Relevant(["style", "analyzers"], null));
        Assert.Equal(new[] { string.Empty }, FormatCommand.Relevant([string.Empty], ["BRO1306"]));
        Assert.Null(FormatCommand.FindingFiles(lines.Append($@"{Path.Combine(Path.GetDirectoryName(root)!, "Elsewhere.cs")}(1,1): warning BRO1001: x [y]"), root));
    }
}
