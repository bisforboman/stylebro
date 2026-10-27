using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fixture matrix (scripts/fixtures) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void CheckBuild_BuildsTheCopyOfHead_WithTheRepositorysHistory()
    {
        // git-height-shallow: in a shallow clone Nerdbank.GitVersioning fails every build ("Shallow clone lacks the objects
        // required to calculate version height"); the copy of HEAD had no git at all, built, and check --build said "Not clean"
        // for errors the untouched code has too. The copy is a repository with the clone's history, shallow boundary included.
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n");
        Write("Lib/C.cs", "namespace Lib;\n\npublic static class C\n{\n}\n");
        Write("Lib.slnx", "<Solution>\n  <Project Path=\"Lib/Lib.csproj\" />\n</Solution>\n");
        PreviewCommand.MakeRepository(root);
        Write("Lib/D.cs", "namespace Lib;\n\npublic static class D\n{\n}\n");
        PreviewCommand.MakeRepository(root);
        var clone = Path.Combine(root, "..", Path.GetFileName(root) + "-shallow");
        try
        {
            Assert.Equal(0, PreviewCommand.Git(root, "clone", "-q", "--depth", "1", $"file://{root}", clone).Code);
            var head = PreviewCommand.Git(clone, "rev-parse", "HEAD").Output.Trim();
            File.WriteAllText(Path.Combine(clone, "Lib", "C.cs"), "namespace Lib;\n\ninternal static class C\n{\n}\n"); // format's change
            var seen = new List<(string Head, string Count, string Status)>();
            (int, IReadOnlyList<string>) Build(string folder, string workspace, string configuration)
            {
                if (Path.GetFullPath(folder) != Path.GetFullPath(clone))
                {
                    seen.Add((PreviewCommand.Git(folder, "rev-parse", "HEAD").Output.Trim(), PreviewCommand.Git(folder, "rev-list", "--count", "HEAD").Output.Trim(), PreviewCommand.Git(folder, "status", "--porcelain", "--untracked-files=no").Output.Trim()));
                }

                return (0, []);
            }

            FormatCommand.BuildCheckLine(clone, Path.Combine(clone, "Lib.slnx"), _ => { }, Build);

            Assert.NotEmpty(seen);
            Assert.All(seen, s => Assert.Equal((head, "1", string.Empty), s));
        }
        finally
        {
            DeleteFolder(clone);
        }
    }
}
