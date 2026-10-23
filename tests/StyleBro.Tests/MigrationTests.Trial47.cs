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

    [Fact]
    public void Format_RunsTheConfigurationsThatGenerateDocsOfTheirOwn()
    {
        // zio: GenerateDocumentationFile only in Release, where BRO1601 reports; format ran Debug alone and said clean, and the
        // Release TWAE build failed on 128 BRO1601 findings.
        Assert.True(FormatCommand.DocumentsByConfiguration("<PropertyGroup Condition=\"'$(Configuration)'=='Release'\">\n  <GenerateDocumentationFile>true</GenerateDocumentationFile>\n</PropertyGroup>"));
        Assert.True(FormatCommand.DocumentsByConfiguration("<PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' \">\n  <DocumentationFile>bin\\Release\\App.xml</DocumentationFile>\n</PropertyGroup>"));
        Assert.True(FormatCommand.DocumentsByConfiguration("<GenerateDocumentationFile Condition=\"'$(Configuration)' == 'Release'\">true</GenerateDocumentationFile>"));
        Assert.False(FormatCommand.DocumentsByConfiguration("<PropertyGroup Condition=\"'$(Configuration)' == 'Release'\"><Optimize>true</Optimize></PropertyGroup>\n<PropertyGroup><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup>"));
        Assert.False(FormatCommand.DocumentsByConfiguration("<PropertyGroup Condition=\"'$(TargetFramework)' == 'net8.0'\"><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup>"));

        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup Condition=\"'$(Configuration)'=='Release'\">\n    <GenerateDocumentationFile>true</GenerateDocumentationFile>\n  </PropertyGroup>\n</Project>\n");
        Assert.True(FormatCommand.CompileByConfiguration(Path.Combine(root, "src", "Lib", "Lib.csproj")));

        // The configuration's run: the same files and symbols, but docs generated, so a key of its own.
        Write("src/Lib/A.cs", "class A { }\n");
        var file = Path.Combine(root, "src", "Lib", "A.cs");
        var debug = FormatCommand.ReadView("{\"Properties\":{\"DefineConstants\":\"DEBUG\",\"GenerateDocumentationFile\":\"\"},\"Items\":{\"Compile\":[{\"FullPath\":" + System.Text.Json.JsonSerializer.Serialize(file) + "}]}}")!;
        var release = FormatCommand.ReadView("{\"Properties\":{\"DefineConstants\":\"RELEASE\",\"GenerateDocumentationFile\":\"true\"},\"Items\":{\"Compile\":[{\"FullPath\":" + System.Text.Json.JsonSerializer.Serialize(file) + "}]}}")!;
        Assert.False(debug.Documentation);
        Assert.True(release.Documentation);
        Assert.NotEqual(debug.Key(debug.Compile), release.Key(release.Compile));
        Assert.Equal(debug.Key(debug.Compile), (debug with { Defines = ["RELEASE"] }).Key(debug.Compile));
    }
}
