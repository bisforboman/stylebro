using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-second trial (error-or, DbUp, figgle, EasyNetQ) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void ACommittedPackageNextToTheSolution_DoesNotHideTheProjects()
    {
        // DbUp commits src/dbup.1.2.3.nupkg next to src/DbUp.sln and src/Directory.Build.props (none at the root). src/ was
        // taken for an extracted package and skipped: init judged its conventions from no code and pinned the preset in a new
        // root Directory.Build.props nothing imports, and the preview added the package there, so format failed.
        Write("src/DbUp.sln", string.Empty);
        Write("src/Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>\n  </PropertyGroup>\n</Project>\n");
        Write("src/dbup.1.2.3.nupkg", "zip");
        Write("src/dbup-core/dbup-core.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/dbup-core/Own.cs", "namespace DbUp;\n\npublic class Own\n{\n}\n");
        Write("src/new.cs", "class New { }\n");

        Assert.Equal(new[] { Path.Combine(root, "src", "Directory.Build.props") }, Migration.PropsFiles(root));
        Assert.Contains(StyleCopSetup.EnumerateFiles(root), f => f.EndsWith("Own.cs", StringComparison.Ordinal));
        Assert.False(PreviewCommand.InPackage(root, "src/new.cs"));

        // An extracted package is still one (trial 31's packages folders).
        Write("packages/Old.Lib.1.0.0/Old.Lib.1.0.0.nupkg", "zip");
        Write("packages/Old.Lib.1.0.0/content/Helper.cs", "class Helper { }\n");
        Assert.DoesNotContain(StyleCopSetup.EnumerateFiles(root), f => f.EndsWith("Helper.cs", StringComparison.Ordinal));
        Assert.True(PreviewCommand.InPackage(root, "packages/Old.Lib.1.0.0/content/Helper.cs"));
    }
}
