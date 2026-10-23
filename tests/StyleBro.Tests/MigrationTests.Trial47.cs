using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the forty-seventh trial (CSharpFunctionalExtensions, zio, SmartFormat, dailydevops/fluentvalue) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Delete_AFolderThatWasNeverMade_SaysNothing()
    {
        // zio: after a failed restore 'dotnet format' wrote no --report folder, and format --json said "Couldn't delete the copy".
        var missing = Path.Combine(root, "stylebro-format-json-missing");
        Assert.Equal(string.Empty, Capture(() => PreviewCommand.Delete(missing)));

        var made = Directory.CreateDirectory(Path.Combine(root, "stylebro-format-json-made")).FullName;
        File.WriteAllText(Path.Combine(made, "format-report.json"), "[]");
        Assert.Equal(string.Empty, Capture(() => PreviewCommand.Delete(made)));
        Assert.False(Directory.Exists(made));
    }
}
