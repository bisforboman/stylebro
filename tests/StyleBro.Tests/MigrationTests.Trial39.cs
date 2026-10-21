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

    [Fact]
    public void PackageTargets_HideASubmodulesProject_WhenTheSuperprojectsSolutionBuildsIt()
    {
        // MailKit: MailKit.sln lists submodules/MimeKit's project, and its build reported 145,994 findings there that format
        // leaves alone. Built on its own, or from a solution inside the submodule, its findings stay (trial 28).
        Write(".git/HEAD", "ref: refs/heads/main\n");
        Write(".gitmodules", "[submodule \"ext/lib\"]\n\tpath = ext/lib\n\turl = ../lib\n");
        Write("ext/lib/.git", "gitdir: ../../.git/modules/ext/lib\n");
        var targets = Path.Combine(Presets.Root, "src", "StyleBro.Package", "build", "StyleBro.Analyzers.targets");
        Write("ext/lib/src/Lib/Lib.csproj", $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n  <Import Project=\"{targets}\" />\n</Project>\n");
        string Submodules(params string[] properties) => FormatCommand.Capture(root, ["msbuild", Path.Combine(root, "ext", "lib", "src", "Lib", "Lib.csproj"), "-target:_StyleBroFindSubmodules", "-getProperty:StyleBroSubmoduleFolders", "-nologo", .. properties]).Trim();

        Assert.Equal(string.Empty, Submodules());
        Assert.Equal(string.Empty, Submodules($"-property:SolutionDir={Path.Combine(root, "ext", "lib")}{Path.DirectorySeparatorChar}"));
        Assert.Equal(Path.Combine(root, "ext", "lib") + Path.DirectorySeparatorChar, Submodules($"-property:SolutionDir={root}{Path.DirectorySeparatorChar}"));
    }

    [Fact]
    public void Migration_UsingGroups_FollowTheCode()
    {
        // Open-XML-SDK: blankLinesBetweenUsingGroups = require became dotnet_separate_import_directive_groups = true, and the
        // formatter put a blank line between root namespaces in 342 StyleCop-clean files (557 files had none, 4 had them all).
        Write("stylecop.json", "{ \"settings\": { \"orderingRules\": { \"blankLinesBetweenUsingGroups\": \"require\", \"systemUsingDirectivesFirst\": false } } }\n");
        foreach (var name in new[] { "A", "B", "C" })
        {
            Write($"{name}.cs", $"using Alpha;\nusing System;\nusing System.IO;\n\nclass {name}\n{{\n}}\n");
        }

        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Contains("dotnet_separate_import_directive_groups = false", result.Lines);
        Assert.Contains(result.Notes, n => n.StartsWith("Using groups:", StringComparison.Ordinal) && n.Contains("0 files with a blank line between root namespaces, 3 without", StringComparison.Ordinal));

        // Where the code separates them, the key stays.
        foreach (var name in new[] { "A", "B", "C" })
        {
            Write($"{name}.cs", $"using Alpha;\n\nusing System;\nusing System.IO;\n\nclass {name}\n{{\n}}\n");
        }

        Assert.Contains("dotnet_separate_import_directive_groups = true", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void SeparatesRootNamespaces_IsTrueFalseOrMixed()
    {
        static bool? Separates(string usings) =>
            Conventions.SeparatesRootNamespaces([[.. ((Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(usings + "class A { }").GetRoot()).Usings]]);

        Assert.True(Separates("using Alpha;\n\nusing System;\nusing System.IO;\n"));
        Assert.False(Separates("using Alpha;\nusing System;\n"));
        Assert.Null(Separates("using Alpha;\n\nusing Beta;\nusing System;\n"));
        Assert.Null(Separates("using System;\nusing System.IO;\nusing static System.Math;\n"));
        Assert.True(Migration.SeparatesUsingGroups(4, 0));
        Assert.False(Migration.SeparatesUsingGroups(4, 557));
        Assert.False(Migration.SeparatesUsingGroups(2, 0));
    }
}
