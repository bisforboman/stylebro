using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-ninth trial (SharpYaml, Bullseye, ReportGenerator, DelegateDecompiler) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void NotLoaded_NamesTheDirectoryBuildPropsThatShadowsTheReference()
    {
        // DelegateDecompiler: src/Directory.Build.props doesn't import a parent's, none at the root. The reference went into a
        // new root Directory.Build.props that no project reads, and format only said to add the package.
        Write("Directory.Build.props", "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"1.0.0\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n");
        Write("src/Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>\n  </PropertyGroup>\n</Project>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib.Tests/Lib.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("tools/Tool/Tool.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        var projects = new[] { "src/Lib/Lib.csproj", "src/Lib.Tests/Lib.Tests.csproj" }.Select(p => Path.Combine(root, p)).ToList();

        var shadowed = Migration.ShadowedReferences(root, projects);

        Assert.Equal(new[] { ("Directory.Build.props", "src/Directory.Build.props") }, shadowed);
        var message = FormatCommand.NotLoadedMessage(new[] { "src/Lib/Lib.csproj", "src/Lib.Tests/Lib.Tests.csproj" }, all: true, shadowed)!;
        Assert.StartsWith(FormatCommand.NotLoadedHeader, message, StringComparison.Ordinal);
        Assert.Contains("put the reference in src/Directory.Build.props", message, StringComparison.Ordinal);
        Assert.DoesNotContain("Add the StyleBro.Analyzers package", message, StringComparison.Ordinal);

        // The project under tools/ reads the root's: nothing is shadowed there.
        Assert.Empty(Migration.ShadowedReferences(root, new[] { Path.Combine(root, "tools/Tool/Tool.csproj") }));

        // Once src/'s imports its parent's, the root's reference is read.
        Write("src/Directory.Build.props", "<Project>\n  <Import Project=\"$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))\" />\n</Project>\n");
        Assert.Empty(Migration.ShadowedReferences(root, projects));

        // A reference in a comment is no reference; without one the message stays as it was.
        Write("src/Directory.Build.props", "<Project />\n");
        Write("Directory.Build.props", "<Project>\n  <!-- <PackageReference Include=\"StyleBro.Analyzers\" /> -->\n</Project>\n");
        Assert.Empty(Migration.ShadowedReferences(root, projects));
        Assert.Contains("Add the StyleBro.Analyzers package", FormatCommand.NotLoadedMessage(new[] { "src/Lib/Lib.csproj" }, all: true, [])!, StringComparison.Ordinal);
    }

    [Fact]
    public void TheHeadCopy_HasTheRepositorysTags()
    {
        // SharpYaml: MinVer versions the assembly from the tags. check --build's copy of HEAD had none, built as 0.0.0.0, and
        // the CS0281 its untouched Debug build has (it names that version) counted as new: "Not clean".
        var repository = Path.Combine(root, "repo");
        Write("repo/A.cs", "class A { }\n");
        Assert.Equal(0, PreviewCommand.Git(repository, "init", "-q").Code);
        Assert.Equal(0, PreviewCommand.Git(repository, "add", "-A").Code);
        Assert.Equal(0, PreviewCommand.Git(repository, "-c", "user.name=t", "-c", "user.email=t@example.com", "commit", "-qm", "one").Code);
        Assert.Equal(0, PreviewCommand.Git(repository, "tag", "v1.0.0").Code);
        Assert.Equal(0, PreviewCommand.Git(repository, "-c", "user.name=t", "-c", "user.email=t@example.com", "tag", "-a", "v3.16.0", "-m", "annotated").Code);
        var copy = Path.Combine(root, "copy");
        Write("copy/A.cs", "class A { }\n");

        FormatCommand.WithHistory(copy, repository);

        Assert.Equal("v1.0.0\nv3.16.0", PreviewCommand.Git(copy, "tag").Output.Replace("\r\n", "\n").Trim());
        Assert.Equal("tag", PreviewCommand.Git(copy, "cat-file", "-t", "v3.16.0").Output.Trim());
        Assert.Equal(PreviewCommand.Git(repository, "rev-parse", "HEAD").Output, PreviewCommand.Git(copy, "rev-parse", "HEAD").Output);
    }
}
