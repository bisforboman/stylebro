using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void Preview_WritesThePatchIntoTheGitFolder_NotTheWorkingTree()
    {
        // Trial 17: stylebro-preview.patch in the repository root showed up as an untracked file.
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/C.cs", "class C\n{\n}\n");
        PreviewCommand.MakeRepository(root);
        int Format(string[] args, Action<string> log)
        {
            var report = Array.IndexOf(args, "--report");
            if (report >= 0)
            {
                Directory.CreateDirectory(args[report + 1]);
                File.WriteAllText(Path.Combine(args[report + 1], "format-report.json"), "[]");
            }

            return 0;
        }

        var output = Capture(() => Assert.Equal(0, PreviewCommand.Run("format", new[] { Path.Combine(root, "App/App.csproj"), "--diff" }, Format)));

        Assert.True(File.Exists(Path.Combine(root, ".git", PreviewCommand.DefaultPatch)));
        Assert.False(File.Exists(Path.Combine(root, PreviewCommand.DefaultPatch)));
        Assert.Contains(Path.Combine(root, ".git", PreviewCommand.DefaultPatch), output, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, PreviewCommand.Git(root, "status", "--porcelain", "--untracked-files=all").Output.Trim());

        // Outside a git repository: the temp folder.
        var plain = Directory.CreateTempSubdirectory("stylebro-no-git-").FullName;
        try
        {
            Assert.Equal(Path.Combine(Path.GetTempPath(), PreviewCommand.DefaultPatch), PreviewCommand.DefaultPatchPath(plain));
        }
        finally
        {
            Directory.Delete(plain);
        }
    }

    [Fact]
    public void FormatOutput_ExplainsFSharpProjectsAndWorkspaceWarnings_Once()
    {
        // FakeItEasy: every run and framework printed both again, unexplained.
        FormatCommand.ResetExplained();
        const string fsharp = "Could not format 'C:\\r\\tests\\Helpers.FSharp\\Helpers.FSharp.fsproj'. Format currently supports only C# and Visual Basic projects.";
        const string warnings = "Warnings were encountered while loading the workspace. Set the verbosity option to the 'diagnostic' level to log warnings.";

        Assert.StartsWith("Not formatted: Helpers.FSharp.fsproj: 'dotnet format' formats C# and Visual Basic projects only", FormatCommand.Explain(fsharp), StringComparison.Ordinal);
        Assert.Null(FormatCommand.Explain(fsharp));
        Assert.StartsWith("'dotnet format' had warnings loading the workspace", FormatCommand.Explain(warnings), StringComparison.Ordinal);
        Assert.Null(FormatCommand.Explain(warnings));
        Assert.Equal("A.cs(1,1): warning BRO1001", FormatCommand.Explain("A.cs(1,1): warning BRO1001"));
    }
}
