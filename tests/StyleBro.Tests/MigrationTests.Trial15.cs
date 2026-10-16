using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifteenth trial (Nerdbank.Streams, Fluid, Moonglade, recyclarr) found in the tool.</summary>
public sealed partial class MigrationTests
{
    private const string GlobalStyleCop = "<Project><ItemGroup><GlobalPackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" /></ItemGroup></Project>";

    [Fact]
    public void AnUnquotedGetPathOfFileAbove_IsFollowed()
    {
        // Nerdbank.Streams (AArnott's Library.Template): test/Directory.Packages.props imports the root's, which has StyleCop.
        Write("Directory.Packages.props", GlobalStyleCop);
        Write("test/Directory.Packages.props", "<Project>\n  <Import Project=\"$([MSBuild]::GetPathOfFileAbove($(MSBuildThisFile), $(MSBuildThisFileDirectory)..))\" />\n</Project>");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("test/Lib.Tests/Lib.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var (uses, _, _, _) = StyleCopSetup.Projects(root, StyleCopSetup.EnumerateFiles(root).ToList());
        Assert.All(uses.Values, Assert.True);
        Assert.Equal([Path.Combine(root, "Directory.Packages.props")], StyleCopSetup.Imports(Path.Combine(root, "test", "Directory.Packages.props"), root));

        // Quoted, or with the start folder quoted, too.
        Write("test/Directory.Packages.props", "<Project><Import Project=\"$([MSBuild]::GetPathOfFileAbove('Directory.Packages.props', '$(MSBuildThisFileDirectory)../'))\" /></Project>");
        Assert.Equal([Path.Combine(root, "Directory.Packages.props")], StyleCopSetup.Imports(Path.Combine(root, "test", "Directory.Packages.props"), root));
    }

    [Fact]
    public void WithSeveralProjects_MSBuildDecidesForEveryOne()
    {
        // The text found StyleCop in both (unconditionally); MSBuild's answer wins anyway: the text keeps missing imports.
        Write("Directory.Packages.props", GlobalStyleCop);
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("test/Lib.Tests/Lib.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var evaluated = new System.Collections.Concurrent.ConcurrentBag<string>();

        var setup = StyleCopSetup.Read(root, project =>
        {
            evaluated.Add(Path.GetFileName(project));
            return !project.Contains("Tests", StringComparison.Ordinal);
        });
        Assert.Equal(["Lib.Tests.csproj", "Lib.csproj"], evaluated.Order(StringComparer.Ordinal));
        Assert.Equal(["src"], setup.Folders);
        Assert.Equal(["test"], setup.FoldersWithout);

        // MSBuild failing: the text's answer, with a warning that says so.
        setup = StyleCopSetup.Read(root, _ => null);
        Assert.Null(setup.Folders);
        Assert.Contains(setup.Warnings, w => w.Contains("an import this reader can't follow is missed", StringComparison.Ordinal));

        // One project: the text is enough.
        File.Delete(Path.Combine(root, "test", "Lib.Tests", "Lib.Tests.csproj"));
        Assert.False(StyleCopSetup.Read(root, _ => throw new InvalidOperationException("evaluated")).NotInstalled);
    }

    [Fact]
    public void NoStyleCopFolders_TheReportSaysWhatHappensThere()
    {
        Write("Directory.Packages.props", GlobalStyleCop);
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("test/Lib.Tests/Lib.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var report = Program.ScopeReport(StyleCopSetup.Read(root, project => !project.Contains("Tests", StringComparison.Ordinal)));
        Assert.Contains(report, l => l.StartsWith("No StyleCop in test: StyleBro's rules and the built-in ones the block sets are off there by id", StringComparison.Ordinal) && l.Contains("remove the block's '# No StyleCop ran in' line", StringComparison.Ordinal));
        Assert.DoesNotContain(report, l => l.Contains("init' can set those up", StringComparison.Ordinal));
    }

    [Fact]
    public void Init_ACommentOnlyUsingVerdict_IsWrittenOnceInItsPlace()
    {
        // Fluid: "using directives: ... so they aren't sorted" has no setting line; a second init kept it with BRO1306's
        // comment below it and wrote it again.
        for (var i = 0; i < 6; i++)
        {
            var name = i % 2 == 0 ? $"Max{i}" : $"max{i}";
            Write($"A{i}.cs", $"using System.Text;\nusing System;\n\nclass A{i}\n{{\n    private const int {name} = 1;\n}}\n");
        }

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("# init: using directives:", first, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.BRO1306.severity = none", first, StringComparison.Ordinal);

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void KeptLines_LeaveAnotherTopicsCommentAbove()
    {
        var previous = new[]
        {
            "# init: using directives: 5 of 6 files not sorted, so they aren't sorted",
            "# init: private constants/static readonly fields: your code mixes their casing",
            "dotnet_diagnostic.BRO1306.severity = none",
        };

        Assert.Equal(previous[1..], InitCommand.KeptLines(previous, ["dotnet_diagnostic.BRO1306.severity"]));
    }

    [Fact]
    public void Verify_ALineEndingOnlyChange_IsNoChange()
    {
        Write("a.cs", "class A\r\n{\r\n}\r\n");
        Assert.True(FormatCommand.SameButEndings("class A\n{\n}\n", Path.Combine(root, "a.cs")));
        Assert.False(FormatCommand.SameButEndings("class A\n{\n\n}\n", Path.Combine(root, "a.cs")));
        Assert.False(FormatCommand.SameButEndings("class A\n{\n}\n", Path.Combine(root, "missing.cs")));
    }

    [Fact]
    public void ThePackageVersion_DoesNotSplitACommentFromItsItem()
    {
        // Fluid: StyleBro's line went between '<!-- Only used with netstandard2.0 -->' and System.Linq.Async.
        var packages = "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"Microsoft.Extensions.FileProviders.Abstractions\" Version=\"8\" />\n    <!-- Only used with netstandard2.0 -->\n    <PackageVersion Include=\"System.Linq.Async\" Version=\"6.0.3\" />\n  </ItemGroup>\n</Project>\n";
        Assert.Equal(
            "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"Microsoft.Extensions.FileProviders.Abstractions\" Version=\"8\" />\n    <PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />\n    <!-- Only used with netstandard2.0 -->\n    <PackageVersion Include=\"System.Linq.Async\" Version=\"6.0.3\" />\n  </ItemGroup>\n</Project>\n",
            PreviewCommand.AddPackageVersion(packages, "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />"));

        // A comment over several lines goes along too.
        var multiLine = packages.Replace("<!-- Only used with netstandard2.0 -->", "<!--\n      Only used with\n      netstandard2.0 -->", StringComparison.Ordinal);
        Assert.Contains("Version=\"8\" />\n    <PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />\n    <!--\n", PreviewCommand.AddPackageVersion(multiLine, "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />"), StringComparison.Ordinal);
    }

    [Fact]
    public void Init_NamesTheDirectoryBuildPropsTheProjectsImport()
    {
        // Moonglade has only src/Directory.Build.props; init named 'Directory.Build.props'.
        Write("src/Directory.Build.props", "<Project />");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("src/App/A.cs", "class A\n{\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Contains("<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild> in src/Directory.Build.props.", output, StringComparison.Ordinal);
    }
}
