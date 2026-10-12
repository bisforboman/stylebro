using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Findings of the ninth trial for the tool (kubernetes-client, tfplan2md, Blazor.Diagrams, MetadataExtractor).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Format_ProjectsThatCompileTheSameFiles_RunApart_WithWhatTheyReference()
    {
        // kubernetes-client: KubernetesClient.Aot compiles ..\KubernetesClient\*.cs; loaded in one run, Roslyn's linked-file
        // merge crashed (dotnet/roslyn#86001) after writing 242 files.
        var compile = new Dictionary<string, string[]>
        {
            ["E2E.Aot.Tests"] = new[] { "e2e.cs" },
            ["KubernetesClient"] = new[] { "Kubernetes.cs", "Models.cs" },
            ["KubernetesClient.Aot"] = new[] { "models.cs" }, // paths compare like Windows paths
            ["KubernetesClient.Tests"] = new[] { "tests.cs" },
            ["Other"] = new[] { "other.cs" },
        };
        var references = new Dictionary<string, string[]>
        {
            ["E2E.Aot.Tests"] = new[] { "KubernetesClient.Aot" },
            ["KubernetesClient.Tests"] = new[] { "KubernetesClient" },
        };
        IEnumerable<string> Refs(string p) => references.TryGetValue(p, out var r) ? r : Array.Empty<string>();

        var groups = FormatCommand.Separate(compile.Keys, Refs, p => compile[p]);

        Assert.Equal(new[] { "E2E.Aot.Tests", "KubernetesClient.Aot", "Other" }, groups[0]);
        Assert.Equal(new[] { "KubernetesClient", "KubernetesClient.Tests" }, groups[1]);
        Assert.Equal(2, groups.Count);
        Assert.Single(FormatCommand.Separate(new[] { "KubernetesClient.Tests", "Other" }, Refs, p => compile[p]));
        Assert.Equal(new[] { "KubernetesClient", "KubernetesClient.Aot" }, FormatCommand.SharingProjects(compile.Select(c => (Path.Combine(root, c.Key + ".csproj"), c.Value))));
    }

    [Fact]
    public void Format_ACrashedRun_NamesItsProjectsAndTheSdkBug()
    {
        Assert.True(FormatCommand.IsCrash("Unhandled exception: System.AggregateException: One or more errors occurred."));
        Assert.False(FormatCommand.IsCrash("  Unhandled exception in a file name"));
        var message = FormatCommand.CrashMessage(new[] { "KubernetesClient.Aot", "KubernetesClient" });
        Assert.Contains("in the run for KubernetesClient, KubernetesClient.Aot", message, StringComparison.Ordinal);
        Assert.Contains(FormatCommand.LinkedMergeIssue, message, StringComparison.Ordinal);
    }

    [Fact]
    public void Workspace_TheOnlySolutionOneFolderDown_IsUsed()
    {
        // tfplan2md keeps its only solution in src/: "No solution or project".
        Write("src/tfplan2md.slnx", "<Solution />");
        Write("src/App/App.csproj", "<Project />");
        Write("bin/Old.sln", string.Empty);

        Assert.Equal(Path.Combine("src", "tfplan2md.slnx"), BaselineCommand.FindWorkspace(root));
        Assert.True(BaselineCommand.IsBelow(BaselineCommand.FindWorkspace(root)!));
        Assert.Equal("Using src/tfplan2md.slnx: no solution or project at the root, and it's the only solution one folder down.", BaselineCommand.BelowNote(Path.Combine("src", "tfplan2md.slnx")));

        // Several: asked, as at the root.
        Write("tools/Tools.sln", string.Empty);
        Assert.Null(BaselineCommand.FindWorkspace(root));
        Assert.Contains("Several solutions or projects", BaselineCommand.NoWorkspace(root, "--project <file>"), StringComparison.Ordinal);

        // One at the root wins.
        Write("Root.csproj", "<Project />");
        Assert.Equal("Root.csproj", BaselineCommand.FindWorkspace(root));
    }

    [Fact]
    public void Workspace_ATraversalProject_IsOne()
    {
        // kubernetes-client builds through kubernetes-client.proj (Microsoft.Build.Traversal) and has no solution.
        Write("kubernetes-client.proj", "<Project Sdk=\"Microsoft.Build.Traversal\">\n  <ItemGroup>\n    <ProjectReference Include=\"src/**/*.csproj\" />\n  </ItemGroup>\n</Project>\n");
        Write("build.proj", "<Project />");
        Write("tests/A.Tests/A.Tests.slnx", "<Solution />");

        Assert.Equal("kubernetes-client.proj", BaselineCommand.FindWorkspace(root));
        Assert.True(FormatCommand.IsTraversal(Path.Combine(root, "kubernetes-client.proj")));
        Assert.False(FormatCommand.IsTraversal(Path.Combine(root, "build.proj")));
        Assert.Equal(
            "<Solution>\n  <Project Path=\"src/A/A.csproj\" />\n  <Project Path=\"tests/A &amp; B/T.csproj\" />\n</Solution>\n",
            FormatCommand.SolutionText(root, new[] { Path.Combine(root, "src", "A", "A.csproj"), Path.Combine(root, "tests", "A & B", "T.csproj") }));
    }

    [Fact]
    public void AddPackage_AProjectThatTurnsCentralPackageManagementOnItself_GetsNoVersion()
    {
        // Blazor.Diagrams: site/Site/Site.csproj sets ManagePackageVersionsCentrally itself, under no Directory.Build.props
        // that does; the root Directory.Build.props the preview created had a Version: NU1008.
        Write("Directory.Packages.props", "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"xunit\" Version=\"2.6.3\" />\n  </ItemGroup>\n</Project>\n");
        Write("site/Site/Site.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>\n");
        Write("tools/Tool/Tool.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        PreviewCommand.AddPackage(root);

        var props = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
        Assert.Contains("<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" Condition=\"'$(ManagePackageVersionsCentrally)' == 'true'\" />", props, StringComparison.Ordinal);
        Assert.Matches("<PackageReference Include=\"StyleBro.Analyzers\" Version=\"[^\"]+\" PrivateAssets=\"all\" Condition=\"'\\$\\(ManagePackageVersionsCentrally\\)' != 'true'\" />", props);
        Assert.Contains("<PackageVersion Include=\"StyleBro.Analyzers\"", File.ReadAllText(Path.Combine(root, "Directory.Packages.props")), StringComparison.Ordinal);
    }

    [Fact]
    public void Init_DryRunOnAMigratedRepository_SaysWhatWriteSays()
    {
        // mdx: the dry run printed a block "to add", --write then said the settings were already there.
        Write(".editorconfig", Migration.Apply("root = true\n", Migration.Render(new[] { ("[*.cs]", new List<string> { "dotnet_diagnostic.IDE0040.severity = none" }) })));

        var dry = Capture(() => InitCommand.Run(new[] { root }));
        var write = Capture(() => InitCommand.Run(new[] { root, "--write" }));

        Assert.Contains(InitCommand.MigratedLine, dry, StringComparison.Ordinal);
        Assert.Contains(InitCommand.MigratedLine, write, StringComparison.Ordinal);
        Assert.DoesNotContain("run with --write to add it", dry, StringComparison.Ordinal);
        Assert.DoesNotContain("IDE0036", dry, StringComparison.Ordinal);

        // Settings an earlier init wrote: the same.
        File.Delete(Path.Combine(root, ".editorconfig"));
        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));
        dry = Capture(() => InitCommand.Run(new[] { root }));
        write = Capture(() => InitCommand.Run(new[] { root, "--write" }));
        Assert.Contains(InitCommand.PresentLine, dry, StringComparison.Ordinal);
        Assert.Contains(InitCommand.PresentLine, write, StringComparison.Ordinal);
        Assert.DoesNotContain("run with --write to add it", dry, StringComparison.Ordinal);
    }

    [Fact]
    public void EditedFiles_KeepTheirLineEndings()
    {
        // mdx: init rewrote a CRLF .editorconfig as LF.
        Write(".editorconfig", "root = true\r\n\r\n[*.cs]\r\nindent_size = 4\r\n");

        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));

        var text = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("dotnet_diagnostic.IDE0036.severity", text, StringComparison.Ordinal);
        Assert.DoesNotMatch("[^\r]\n", text);
        Assert.Equal("a\nb\n", PreviewCommand.KeepLineEndings("x\ny\n", "a\r\nb\n"));
        Assert.Equal("a\r\nb\r\n", PreviewCommand.KeepLineEndings("x\r\ny\r\n", "a\r\nb\n"));
        Assert.Equal("a\r\nb\n", PreviewCommand.KeepLineEndings("x\r\ny\n", "a\r\nb\n")); // mixed: as is
        Assert.Equal("a\r\nb\n", PreviewCommand.KeepLineEndings(null, "a\r\nb\n"));
    }
}
