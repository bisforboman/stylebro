using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-seventh trial (Newtonsoft.Json.Bson, OpenAPI.NET, ErrorProne.NET, DapperAOT) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void FirstBuildError_IsTheMessageOfTheFirstErrorLine()
    {
        // DapperAOT's design-time build in a shallow clone: Nerdbank.GitVersioning's task fails, then says why.
        const string output = "  Restore complete.\n"
            + "/x/Lib.csproj : warning NU1603: Something resolved instead. [/x/Lib.csproj]\n"
            + "/nuget/nerdbank.gitversioning/build/Nerdbank.GitVersioning.targets(80,5): error : build ('-2') must be a non-negative value. (Parameter 'build') [/x/Lib.csproj]\n"
            + "/x/Lib.csproj(9,5): error MSB4018: The second one. [/x/Lib.csproj]\n";
        Assert.Equal("build ('-2') must be a non-negative value. (Parameter 'build')", FormatCommand.FirstBuildError(output));
        Assert.Equal("The second one.", FormatCommand.FirstBuildError("/x/Lib.csproj(9,5): error MSB4018: The second one. [/x/Lib.csproj]"));
        Assert.Null(FormatCommand.FirstBuildError("/x/Lib.csproj : warning NU1603: Something. [/x/Lib.csproj]\n"));
    }

    [Fact]
    public void LoadFailureGap_NamesTheRuns_AndCounts()
    {
        var gap = FormatCommand.LoadFailureGap("Dapper.AOT.Analyzers", ["netstandard2.0", "netstandard2.0, Release"], "build ('-2') must be a non-negative value.");
        Assert.Equal("Not checked: Dapper.AOT.Analyzers (netstandard2.0; netstandard2.0, Release): MSBuild failed loading it (build ('-2') must be a non-negative value), so 'dotnet format' had none of its build settings and StyleBro's rules didn't run there", gap);
        Assert.StartsWith("Not checked: Lib: ", FormatCommand.LoadFailureGap("Lib", [], "x"), StringComparison.Ordinal);
        Assert.Single(FormatCommand.Unverified([gap]));
    }

    [Fact]
    public void ThePreviewsCopy_StartsFromTheRepositorysHistory()
    {
        // DapperAOT: the copy's repository had two commits of its own, so Nerdbank.GitVersioning's versionHeightOffset -4 made
        // the build number negative there (and SourceLink found no remote): the analyzer project loaded without its settings,
        // and the preview missed 22 files. The copy borrows the repository's objects, starts from its HEAD, keeps its remote.
        Write("App/C.cs", "class C\n{\n}\n");
        PreviewCommand.MakeRepository(root);
        Write("App/D.cs", "class D\n{\n}\n");
        PreviewCommand.MakeRepository(root); // a second commit (git init again keeps the repository)
        Assert.Equal(0, PreviewCommand.Git(root, "remote", "add", "origin", "https://github.com/example/app.git").Code);
        var head = PreviewCommand.Git(root, "rev-parse", "HEAD").Output.Trim();
        File.WriteAllText(Path.Combine(root, "App", "C.cs"), "class C\n{\n    int x;\n}\n"); // not committed

        var copy = Path.Combine(root, "..", Path.GetFileName(root) + "-copy");
        var shallow = Path.Combine(root, "..", Path.GetFileName(root) + "-shallow");
        var shallowCopy = Path.Combine(root, "..", Path.GetFileName(root) + "-shallow-copy");
        try
        {
            PreviewCommand.Copy(root, copy);
            PreviewCommand.MakeRepository(copy, source: root);

            Assert.Equal("3", PreviewCommand.Git(copy, "rev-list", "--count", "HEAD").Output.Trim());
            Assert.Equal(head, PreviewCommand.Git(copy, "rev-parse", "HEAD~1").Output.Trim());
            Assert.Equal("App/C.cs", PreviewCommand.Git(copy, "diff", "--name-only", "HEAD~1", "HEAD").Output.Trim());
            Assert.Equal("https://github.com/example/app.git", PreviewCommand.Git(copy, "remote", "get-url", "origin").Output.Trim());

            // A shallow clone (CI's checkouts) stays shallow: its boundary comes along, so the history ends where it does there.
            Assert.Equal(0, PreviewCommand.Git(root, "clone", "-q", "--depth", "1", $"file://{root}", shallow).Code);
            PreviewCommand.Copy(shallow, shallowCopy);
            PreviewCommand.MakeRepository(shallowCopy, source: shallow);
            Assert.Equal((0, "2"), (PreviewCommand.Git(shallowCopy, "rev-list", "--count", "HEAD").Code, PreviewCommand.Git(shallowCopy, "rev-list", "--count", "HEAD").Output.Trim()));
        }
        finally
        {
            DeleteFolder(copy);
            DeleteFolder(shallow);
            DeleteFolder(shallowCopy);
        }
    }
}
