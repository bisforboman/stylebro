using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void PreviewCopy_TakesTrackedFilesUnderAnIgnoredFolder()
    {
        // MoreLINQ tracks bld/ExtensionsGenerator although .gitignore ignores bld/: the preview's copy missed it.
        Write(".gitignore", "bld/\n");
        Write("bld/Tool.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("bld/Untracked.cs", "class U { }\n");
        PreviewCommand.MakeRepository(root);
        PreviewCommand.Git(root, "add", "-f", "bld/Tool.csproj");
        PreviewCommand.Git(root, "-c", "user.name=t", "-c", "user.email=t@e.invalid", "commit", "-qm", "tracked");
        var target = Directory.CreateTempSubdirectory("stylebro-preview-test-").FullName;
        try
        {
            var ignoredTracked = PreviewCommand.Copy(root, target);
            PreviewCommand.MakeRepository(target, ignoredTracked);

            Assert.Equal(["bld/Tool.csproj"], ignoredTracked);
            Assert.False(File.Exists(Path.Combine(target, "bld", "Untracked.cs")));

            // The copy's repository tracks it too, so a format run's change to it shows in the preview.
            File.AppendAllText(Path.Combine(target, "bld", "Tool.csproj"), "\n");
            Assert.Contains("bld/Tool.csproj", PreviewCommand.Git(target, "status", "--porcelain").Output, StringComparison.Ordinal);
        }
        finally
        {
            DeleteFolder(target);
        }
    }

    [Fact]
    public void Baseline_RunsEachPassOnItsOwn()
    {
        // One run of all three reported the analyzers' lines after the whitespace pass had split 'if (x) y;': the baseline
        // fingerprinted the wrong lines and the build still failed.
        Assert.Equal(["whitespace", "style", "analyzers"], BaselineCommand.Passes(null));
        Assert.Equal(["style", "analyzers"], BaselineCommand.Passes([]));
        Assert.Equal(["whitespace", "style", "analyzers"], BaselineCommand.Passes(["src/A/"]));
        Assert.Equal(["--verify-no-changes"], FormatCommand.WhitespaceOptions(["--verify-no-changes", "--severity", "warn"], []));
    }

    [Fact]
    public void Format_WorksFromTheFolderHoldingASolutionsProjects()
    {
        // Altinn: solutions/Spot.slnx lists ../src/Lib; from solutions/, '--include ../src/Lib/' matched nothing ("clean").
        Write("solutions/Spot.slnx", "<Solution>\n  <Project Path=\"../src/Lib/Lib.csproj\" />\n</Solution>\n");
        Write("solutions/Spot.sln", "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"Lib\", \"..\\src\\Lib\\Lib.csproj\", \"{0}\"\nEndProject\n");
        Write("solutions/Inside.slnx", "<Solution>\n  <Project Path=\"App/App.csproj\" />\n</Solution>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib/C.cs", "class C\n{\n}\n");

        Assert.Equal(root, FormatCommand.WorkingRoot(Path.Combine(root, "solutions", "Spot.slnx")));
        Assert.Equal(root, FormatCommand.WorkingRoot(Path.Combine(root, "solutions", "Spot.sln")));
        Assert.Equal(Path.Combine(root, "solutions"), FormatCommand.WorkingRoot(Path.Combine(root, "solutions", "Inside.slnx")));
        Assert.Equal(Path.Combine(root, "src", "Lib"), FormatCommand.WorkingRoot(Path.Combine(root, "src", "Lib", "Lib.csproj")));

        // The run's changed files are looked for (and listed) from there.
        var lines = new List<string>();
        var code = FormatCommand.Run([Path.Combine(root, "solutions", "Spot.slnx"), FormatCommand.OnceOption], lines.Add, (_, _) =>
        {
            File.WriteAllText(Path.Combine(root, "src", "Lib", "C.cs"), "class C { }\n");
            return 0;
        });
        Assert.Equal(0, code);
        Assert.Contains("  src/Lib/C.cs", lines);

        lines.Clear();
        Assert.Equal(1, FormatCommand.Run([Path.Combine(root, "solutions", "Nope.slnx")], lines.Add, (_, _) => 0));
        Assert.StartsWith("Not found: ", Assert.Single(lines), StringComparison.Ordinal);
    }

    [Fact]
    public void Format_FixesUnusedUsings_WhereTheRepositoryEnforcesThem()
    {
        // coverlet: IDE0049 ('UInt32' -> 'uint') left 'using System;' unused, and IDE0005 failed the build.
        Write(".editorconfig", "root = true\n\n[*.{cs,vb}]\ndotnet_diagnostic.IDE0005.severity = warning\n");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Assert.Contains(FormatCommand.UnusedUsings, FormatCommand.Diagnostics(Path.Combine(root, "src", "App")));

        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_analyzer_diagnostic.category-Style.severity = error\n");
        Assert.True(FormatCommand.EnforcesUnusedUsings(root));
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_analyzer_diagnostic.severity = warning\ndotnet_diagnostic.IDE0005.severity = suggestion\n");
        Assert.False(FormatCommand.EnforcesUnusedUsings(root));
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_analyzer_diagnostic.severity = warning\n");
        Assert.True(FormatCommand.EnforcesUnusedUsings(root));
        File.Delete(Path.Combine(root, ".editorconfig"));
        Assert.DoesNotContain(FormatCommand.UnusedUsings, FormatCommand.Diagnostics(root));
    }

    [Fact]
    public void Format_SaysToRebuildCheckedInGeneratedFiles_AndToRunCSharpier()
    {
        // MoreLINQ: the build regenerates the checked-in Extensions.g.cs from the doc comments format changed.
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib/Code.cs", "class C { }\n");
        Write("src/Lib/Extensions.g.cs", "class E { }\n");
        Write("src/Lib/Form.Designer.cs", "class F { }\n");
        Write("src/Lib/Table.tt", "<#@ template #>\n");
        Write("src/Lib/Table.cs", "class T { }\n");
        Write("src/Lib/Header.cs", "// <auto-generated>\n//   by a tool\n// </auto-generated>\nclass H { }\n");
        Write("src/Lib/Plain.cs", "class P { }\n");
        Write("src/Other/Other.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Other/Other.g.cs", "class O { }\n");
        Write("build.cmd", "dotnet run --project bld/ExtensionsGenerator > src\\Lib\\Extensions.g.cs\ngen > src\\Other\\Other.g.cs\n");
        PreviewCommand.MakeRepository(root);
        Write("src/Lib/Untracked.g.cs", "class U { }\n");

        var changed = new[] { Path.Combine(root, "src", "Lib", "Code.cs") };
        Assert.Equal(["src/Lib/Extensions.g.cs", "src/Lib/Form.Designer.cs"], FormatCommand.CheckedInGenerated(root, changed));
        Assert.Equal("src/Lib/Extensions.g.cs is generated and checked in: rebuild and commit it.", FormatCommand.AfterNotes(root, changed)[0]);

        // Trial 22: a T4 template's output only when the build runs templates, a header-only file only when a build step names it.
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TransformOnBuild>true</TransformOnBuild></PropertyGroup>\n  <Target Name=\"Gen\"><Exec Command=\"gen -o Header.cs\" /></Target>\n</Project>\n");
        Assert.Equal(["src/Lib/Extensions.g.cs", "src/Lib/Form.Designer.cs", "src/Lib/Header.cs", "src/Lib/Table.cs"], FormatCommand.CheckedInGenerated(root, changed));
        Assert.Empty(FormatCommand.AfterNotes(root, []));
        Assert.DoesNotContain(FormatCommand.AfterNotes(root, changed), n => n.StartsWith("CSharpier", StringComparison.Ordinal));

        Write(".csharpierrc.json", "{}\n");
        Assert.StartsWith("CSharpier formats this repository (.csharpierrc.json): run it now", FormatCommand.AfterNotes(root, changed)[^1], StringComparison.Ordinal);
    }

    [Fact]
    public void PreviewCopy_SaysWhenAnIgnoredNuGetConfigStaysBehind()
    {
        Write(".gitignore", "nuget.config\n");
        Write("nuget.config", "<configuration />\n");
        PreviewCommand.MakeRepository(root);
        var target = Directory.CreateTempSubdirectory("stylebro-preview-test-").FullName;
        try
        {
            PreviewCommand.Copy(root, target);
            Assert.StartsWith("Not copied: nuget.config (git ignores it).", PreviewCommand.IgnoredNuGetConfigNote(root, target), StringComparison.Ordinal);

            File.Copy(Path.Combine(root, "nuget.config"), Path.Combine(target, "nuget.config"));
            Assert.Null(PreviewCommand.IgnoredNuGetConfigNote(root, target));
        }
        finally
        {
            DeleteFolder(target);
        }
    }
}
