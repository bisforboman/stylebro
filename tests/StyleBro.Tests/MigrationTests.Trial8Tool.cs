using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Findings of the eighth trial for the tool (xbox-live-developer-tools, StabilityMatrix, Orleans).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void LineEndings_FilesGitConverts_OutvoteAFewCommittedWithAFixedEnding()
    {
        // xbox: 145 files 'i/lf w/crlf attr/text=auto', 3 committed as CRLF. 'end_of_line = crlf' from the 3 gave 12,593
        // ENDOFLINE errors in an LF checkout.
        Write(".gitattributes", "*.cs -text\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\r\n{{\r\n}}\r\n");
        }

        Git("init", "-q");
        Git("add", ".");
        Git("-c", "user.name=t", "-c", "user.email=t@t", "commit", "-q", "-m", "x");
        Write(".gitattributes", "*.cs text=auto\n");
        for (var i = 0; i < 4; i++)
        {
            Write($"B{i}.cs", $"class B{i}\r\n{{\r\n}}\r\n");
        }

        Git("add", ".");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("end_of_line", StringComparison.Ordinal));
        Assert.Contains("Line endings: git converts 4 of 7 C# files on checkout, so end_of_line isn't set (each machine keeps its own ending; the 3 files committed with a fixed ending don't decide)", result.Notes);

        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.Contains("  git      C# files' line endings: 3 of 3 CRLF -> git converts 4 of 7 C# files on checkout", output);
        Assert.DoesNotContain("end_of_line = ", output);
        Assert.DoesNotContain("note     line endings", output);

        // More files with a fixed ending than converted ones: those decide again.
        for (var i = 3; i < 8; i++)
        {
            Write($"A{i}.cs", $"class A{i}\r\n{{\r\n}}\r\n");
        }

        Assert.Contains("end_of_line = crlf", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Theory]
    [InlineData("CommandLine/App/packages.config", """<?xml version="1.0" encoding="utf-8"?><packages><package id="StyleCop.Analyzers" version="1.0.2" targetFramework="net462" developmentDependency="true" /></packages>""")]
    [InlineData("CommandLine/App/App.csproj", """<Project ToolsVersion="15.0"><ItemGroup><Analyzer Include="..\..\packages\StyleCop.Analyzers.1.0.2\analyzers\dotnet\cs\StyleCop.Analyzers.dll" /></ItemGroup></Project>""")]
    public void StyleCop10_FromPackagesConfigOrAHintPath_TurnsOffRulesItDoesntHave(string file, string content)
    {
        // xbox: SA1135, SA1136, SA1137, SA1139, SA1314 and SA1413 came on and gave 94 findings in StyleCop-clean code.
        Write(file, content);

        var setup = StyleCopSetup.Read(root);

        Assert.Equal("1.0.2", setup.Version);
        foreach (var id in new[] { "SA1135", "SA1136", "SA1137", "SA1139", "SA1314", "SA1413", "SA1141" })
        {
            Assert.False(setup.IsOn(id), id);
        }

        Assert.True(setup.IsOn("SA1642"));
        Assert.Null(StyleCopSetup.OldRules("1.2.0-beta.556"));
        Assert.Contains("SA1413", StyleCopSetup.OldRules("1.1.118")!);
    }

    [Fact]
    public void AddPackage_FollowsANestedPropsImportThroughAProperty()
    {
        // Orleans: src/, test/ and playground/Directory.Build.props import the root's through $(_ParentDirectoryBuildPropsPath).
        // The reference went into all four: NU1504 (duplicate PackageReference).
        const string Nested = """
            <Project>
              <PropertyGroup>
                <_ParentDirectoryBuildPropsPath Condition="'$(_DirectoryBuildPropsFile)' != ''">$([System.IO.Path]::Combine('..', '$(_DirectoryBuildPropsFile)'))</_ParentDirectoryBuildPropsPath>
              </PropertyGroup>
              <Import Project="$(_ParentDirectoryBuildPropsPath)" Condition="Exists('$(_ParentDirectoryBuildPropsPath)')"/>
            </Project>
            """;
        Write("Directory.Build.props", "<Project>\n</Project>\n");
        Write("src/Directory.Build.props", Nested);
        Write("test/Directory.Build.props", Nested);
        Write("src/A/A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("test/B/B.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        Assert.Equal(new[] { "Directory.Build.props" }, Migration.PropsFiles(root).Select(f => Path.GetRelativePath(root, f)));
        Assert.StartsWith("StyleBro.Analyzers ", PreviewCommand.AddPackage(root), StringComparison.Ordinal);
        Assert.Contains("StyleBro.Analyzers", File.ReadAllText(Path.Combine(root, "Directory.Build.props")), StringComparison.Ordinal);
        Assert.DoesNotContain("StyleBro.Analyzers", File.ReadAllText(Path.Combine(root, "src/Directory.Build.props")), StringComparison.Ordinal);
        Assert.DoesNotContain("StyleBro.Analyzers", File.ReadAllText(Path.Combine(root, "test/Directory.Build.props")), StringComparison.Ordinal);
    }

    [Fact]
    public void AddPackage_GivesProjectsWithoutCentralPackageManagementAVersion()
    {
        // StabilityMatrix: analyzers/*.csproj set ManagePackageVersionsCentrally to false; the version-less reference from
        // Directory.Build.props failed their restore (NU1015).
        Write("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>\n");
        Write("Directory.Packages.props", "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"Avalonia\" Version=\"11.0.0\" />\n  </ItemGroup>\n</Project>\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("analyzers/Gen/Gen.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>\n");

        var added = PreviewCommand.AddPackage(root);

        Assert.Contains("with a version for analyzers/Gen/Gen.csproj", added, StringComparison.Ordinal);
        var props = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
        Assert.Contains("<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" Condition=\"'$(ManagePackageVersionsCentrally)' == 'true'\" />", props, StringComparison.Ordinal);
        Assert.Matches("<PackageReference Include=\"StyleBro.Analyzers\" Version=\"[^\"]+\" PrivateAssets=\"all\" Condition=\"'\\$\\(ManagePackageVersionsCentrally\\)' != 'true'\" />", props);
        Assert.Contains("<PackageVersion Include=\"StyleBro.Analyzers\"", File.ReadAllText(Path.Combine(root, "Directory.Packages.props")), StringComparison.Ordinal);
    }

    [Fact]
    public void Format_IncludesTheFilesProjectsLinkFromElsewhere()
    {
        // Orleans: <Compile Include="..\Orleans.CodeGenerator.Shared\X.cs" Link=.../> and src/AdoNet/Shared/** were never
        // formatted in the per-framework runs: '--include' listed the projects' folders only.
        Write("src/A/A.csproj", "<Project />");
        Write("src/Shared/S.cs", "class S { }\n");
        Write("src/B/B.csproj", "<Project />");
        Write("src/B/X.cs", "class X { }\n");
        var json = $$"""{ "Properties": { "TargetFramework": "net8.0" }, "Items": { "Compile": [ { "FullPath": {{System.Text.Json.JsonSerializer.Serialize(Path.Combine(root, "src", "A", "A.cs"))}} }, { "FullPath": {{System.Text.Json.JsonSerializer.Serialize(Path.Combine(root, "src", "Shared", "S.cs"))}} }, { "FullPath": {{System.Text.Json.JsonSerializer.Serialize(Path.Combine(root, "src", "B", "X.cs"))}} } ] } }""";

        var linked = FormatCommand.ParseProject(json, Path.Combine(root, "src", "A")).Linked;

        Assert.Equal(new[] { Path.Combine(root, "src", "Shared", "S.cs"), Path.Combine(root, "src", "B", "X.cs") }, linked);
        Assert.Empty(FormatCommand.ParseProject(json).Linked);

        // A folder without a project as a folder; a file in another project's folder alone (else that project's own files
        // would be formatted in this run too); a file outside the root not at all.
        Assert.Equal(
            new[] { "src/A/", "src/Shared/", "src/B/X.cs" },
            FormatCommand.Include(root, root, new[] { Path.Combine("src", "A", "A.csproj") }, linked.Append(Path.Combine(Path.GetTempPath(), "elsewhere", "Y.cs"))));
    }

    [Fact]
    public void Files_ALinkedFile_IsFormattedInTheProjectThatCompilesIt()
    {
        // Orleans: '--files src/Orleans.CodeGenerator.Shared/X.cs' said "no project file above".
        Write("App/App.csproj", "<Project>\n  <ItemGroup>\n    <Compile Include=\"..\\Shared\\S.cs\" Link=\"S.cs\" />\n  </ItemGroup>\n</Project>\n");
        Write("App/A.cs", "class A { }\n");
        Write("Shared/S.cs", "class S { }\n");
        Git("init", "-q");
        var calls = new List<string[]>();

        var code = FormatCommand.Run(new[] { "--files", Path.Combine(root, "Shared/S.cs"), Path.Combine(root, "App/A.cs"), "--once" }, _ => { }, (args, _) =>
        {
            calls.Add(args);
            return 0;
        });

        Assert.Equal(0, code);
        Assert.Equal(
            new[]
            {
                $"{Path.Combine(root, "App", "App.csproj")} {FormatCommand.WorkingFolderOption} {root} --include Shared/S.cs",
                $"{Path.Combine(root, "App", "App.csproj")} --include A.cs",
            },
            calls.Select(c => string.Join(" ", c)));
    }

    [Fact]
    public void Format_TakesItsDocumentedOptions_InEveryMode()
    {
        // 'format --files a.cs --once' passed --once to 'dotnet format', 'format --diff --project X.sln' passed --project:
        // both printed 'dotnet format''s usage text.
        Assert.Equal(new[] { "X.sln", "--severity", "warn" }, FormatCommand.ProjectAsPath(new[] { "--project", "X.sln", "--severity", "warn" }));
        Assert.Equal(new[] { Path.Combine("repo", "X.sln"), "--once" }, FormatCommand.ProjectAsPath(new[] { "repo", "--project", "X.sln", "--once" }));
        Assert.Equal(new[] { "repo" }, FormatCommand.ProjectAsPath(new[] { "repo" }));

        Write("A/A.csproj", "<Project />");
        Write("A/A.cs", "class A { }\n");
        var calls = new List<string[]>();
        FormatCommand.Run(new[] { "--files", Path.Combine(root, "A/A.cs"), "--once" }, _ => { }, (args, _) =>
        {
            calls.Add(args);
            return 0;
        });
        Assert.DoesNotContain(calls.SelectMany(c => c), a => a == FormatCommand.OnceOption);
    }

    [Fact]
    public void Format_CountsAFileWrittenBackUnchanged_AsUnchanged()
    {
        Write("A.cs", "class A { }\n");
        var before = FormatCommand.Snapshot(root);

        File.WriteAllText(Path.Combine(root, "A.cs"), "class A { }\n");
        File.SetLastWriteTimeUtc(Path.Combine(root, "A.cs"), DateTime.UtcNow.AddMinutes(1));
        Assert.Empty(FormatCommand.ChangedFiles(before, FormatCommand.Snapshot(root)));

        File.WriteAllText(Path.Combine(root, "A.cs"), "class B { }\n");
        Assert.Single(FormatCommand.ChangedFiles(before, FormatCommand.Snapshot(root)));
    }

    [Fact]
    public void Format_SaysOnceWhyProjectsWereSkipped()
    {
        // Orleans: 27 "Partly incomplete" lines, one per test project, all because of an F# project reference.
        Assert.Equal("TestFSharp.fsproj", FormatCommand.UnsupportedProject(@"Could not format 'C:\trial8\orleans\test\Grains\TestFSharp\TestFSharp.fsproj'. Format currently supports only C# and Visual Basic projects."));
        Assert.Null(FormatCommand.UnsupportedProject("Warnings were encountered while loading the workspace."));

        var names = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7" };
        var skipped = names.ToDictionary(n => n, _ => new SortedSet<string> { "net10.0" });
        var frameworks = names.ToDictionary(n => n, _ => new List<string> { "net8.0", "net10.0" });

        var lines = FormatCommand.Incomplete(skipped, frameworks, new[] { "TestFSharp.fsproj" });

        Assert.Equal(2, lines.Count);
        Assert.Equal("Partly incomplete: 'dotnet format' skipped T1, T2, T3, T4, T5 and 2 more for net10.0 (references didn't load): code only that framework compiles (inside #if) wasn't formatted.", lines[0]);
        Assert.StartsWith("  Likely cause: they reference TestFSharp.fsproj, which 'dotnet format' can't load", lines[1], StringComparison.Ordinal);
        Assert.Single(FormatCommand.Incomplete(skipped, frameworks));
    }

    [Fact]
    public void OldStyleProjects_AreNotRestored_AndThePreviewSaysWhyItCant()
    {
        // xbox: format's 'dotnet restore' rewrote project.assets.json and the next Visual Studio build failed ("doesn't list
        // 'win' as a RuntimeIdentifier"); --diff can't restore packages.config at all.
        Write("Old/Old.csproj", "<?xml version=\"1.0\"?>\n<Project ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\n  <Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />\n</Project>\n");
        Write("New/New.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("Imported/Imported.csproj", "<Project>\n  <Import Project=\"Sdk.props\" Sdk=\"Microsoft.NET.Sdk\" />\n</Project>\n");
        Write("App.sln", string.Empty);

        Assert.Equal(new[] { "Old/Old.csproj" }, FormatCommand.OldStyleProjects(root));

        Assert.Equal(1, PreviewCommand.Run("format", new[] { root, "--diff" }, (_, _) => throw new InvalidOperationException("no format")));
        Assert.Contains("--diff doesn't work with old-style (non-SDK) projects (Old/Old.csproj)", PreviewCommand.OldStyleUnsupported(new[] { "Old/Old.csproj" }, "format"), StringComparison.Ordinal);
        Assert.Contains("msbuild -t:restore -p:RestorePackagesConfig=true", PreviewCommand.OldStyleUnsupported(new[] { "Old/Old.csproj" }, null), StringComparison.Ordinal);
    }

    private void Git(params string[] arguments) => Assert.Equal(0, PreviewCommand.Git(root, arguments).Code);
}
