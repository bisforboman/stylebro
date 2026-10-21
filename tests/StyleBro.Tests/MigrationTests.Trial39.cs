using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the thirty-ninth trial (dunet, MailKit, StronglyTypedId, Open-XML-SDK) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void ProjectsIn_TheNotBuiltFolders_AreLeftOutOfTheRuns()
    {
        // StronglyTypedId: the Nuget integration tests are in the .sln without a Build.0 line, and reference a package the
        // repository packs first. 'dotnet format' couldn't load them, and check said "Not checked" about projects no build
        // compiles, though format said it leaves them alone.
        var projects = new[]
        {
            ("Tests", Path.Combine(root, "test", "Tests", "Tests.csproj")),
            ("Nuget.Tests", Path.Combine(root, "test", "Nuget.Tests", "Nuget.Tests.csproj")),
            ("Nuget.Tests.Other", Path.Combine(root, "test", "Nuget.Tests.Other", "Nuget.Tests.Other.csproj")),
            ("Build", Path.Combine(root, "build", "_build.csproj")),
        };

        Assert.Equal(["Nuget.Tests", "Build"], FormatCommand.ProjectsIn(projects, ["build/", "test/Nuget.Tests/"], root));
        Assert.Empty(FormatCommand.ProjectsIn(projects, [], root));
    }
}
