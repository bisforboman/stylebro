using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-first trial (oakton, Pipelines.Sockets.Unofficial, esprima-dotnet, AsyncEx) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Format_AMultiTargetedProjectWithoutTheRunsFramework_LoadsItsOwnFrameworks()
    {
        // Pipelines.Sockets.Unofficial: the tests (net472;net8.0;net10.0) reference Toys (net8.0;net462;net10.0), which
        // references the library (net462;net472;...). In the net472 run Toys was kept (it references a kept project) and
        // forced onto net472, which it doesn't target ("The reference assemblies for .NETFramework,Version=v4.7.2 were not
        // found"): the tests didn't load, a false "Not verified". Here the same shape for net10.0.
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks></PropertyGroup>\n</Project>\n");
        Write("Lib/Lib.cs", "namespace Lib;\n\npublic class A\n{\n}\n");
        Write("Toys/Toys.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks><LangVersion>latest</LangVersion></PropertyGroup>\n  <ItemGroup><ProjectReference Include=\"../Lib/Lib.csproj\" /></ItemGroup>\n</Project>\n");
        Write("Toys/Toys.cs", "namespace Toys;\n\npublic class B\n{\n}\n");
        Write("Tests/Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFrameworks>net10.0</TargetFrameworks></PropertyGroup>\n  <ItemGroup><ProjectReference Include=\"../Lib/Lib.csproj\" /><ProjectReference Include=\"../Toys/Toys.csproj\" /></ItemGroup>\n</Project>\n");
        Write("Tests/Tests.cs", "namespace Tests;\n\npublic class C : Toys.B\n{\n}\n");
        string Full(string name) => Path.Combine(root, name, name + ".csproj");
        var projects = new Dictionary<string, (string[] Frameworks, string[] References)>(StringComparer.OrdinalIgnoreCase)
        {
            [Full("Lib")] = (["netstandard2.0", "net8.0", "net10.0"], []),
            [Full("Toys")] = (["netstandard2.0", "net8.0"], [Full("Lib")]),
            [Full("Tests")] = (["net10.0"], [Full("Lib"), Full("Toys")]),
        };
        var keep = FormatCommand.Keep(projects, "net10.0");
        var other = FormatCommand.Other(projects, keep, "net10.0");
        Assert.Contains(Full("Toys"), keep);
        Assert.Equal(new[] { Full("Toys") }, other);
        Write("select.targets", FormatCommand.SelectFrameworkTargets);

        string List(IEnumerable<string> paths) => "|" + string.Join("|", paths.Select(p => p.ToUpperInvariant())) + "|";
        int Dotnet(bool run, params string[] arguments)
        {
            // Output to a file, not a pipe: a build node that outlives the build would keep a pipe open.
            var log = Path.Combine(root, "build.log");
            var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root };
            foreach (var argument in arguments.Append($"-flp:logfile={log};verbosity=minimal").Append("-nodeReuse:false"))
            {
                start.ArgumentList.Add(argument);
            }

            start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
            start.Environment["UseSharedCompilation"] = "false";
            if (run)
            {
                start.Environment["TargetFramework"] = "net10.0";
                start.Environment["BeforeMicrosoftNETSdkTargets"] = Path.Combine(root, "select.targets");
                start.Environment["StyleBroFormatKeep"] = List(keep);
                start.Environment["StyleBroFormatOther"] = List(other);
            }

            using var process = Process.Start(start)!;
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, File.Exists(log) ? File.ReadAllText(log) : "exit code " + process.ExitCode);
            return process.ExitCode;
        }

        Dotnet(false, "restore", "Tests/Tests.csproj");
        Assert.Equal(0, Dotnet(true, "build", "Tests/Tests.csproj", "--no-restore"));
    }

    [Fact]
    public void Format_ATestProjectWhoseLibrariesReferenceEachOther_Loads()
    {
        // AsyncEx: the tests (netcoreapp3.1) reference Nito.AsyncEx.Context (netstandard1.3;netstandard2.0;net461), which
        // references Nito.AsyncEx.Tasks. The tests' run listed only the tests in its solution filter; the libraries loaded as
        // references, and MSBuildWorkspace gave Context's reference the netstandard1.3 Tasks (no mscorlib): 'dotnet format'
        // skipped the tests ("Required references did not load"), and the build then reported the findings left in them.
        Write("Lib1/Lib1.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFrameworks>netstandard1.3;netstandard2.0</TargetFrameworks></PropertyGroup>\n</Project>\n");
        Write("Lib1/A.cs", "namespace Lib1\n{\n    public class A\n    {\n    }\n}\n");
        Write("Lib2/Lib2.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFrameworks>netstandard1.3;netstandard2.0</TargetFrameworks></PropertyGroup>\n  <ItemGroup><ProjectReference Include=\"../Lib1/Lib1.csproj\" /></ItemGroup>\n</Project>\n");
        Write("Lib2/B.cs", "namespace Lib2\n{\n    public class B : Lib1.A\n    {\n    }\n}\n");
        Write("Tests/Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n  <ItemGroup><ProjectReference Include=\"../Lib2/Lib2.csproj\" /></ItemGroup>\n</Project>\n");
        Write("Tests/C.cs", "namespace Tests\n{\n    public class C : Lib2.B\n    {\n        public int  M() => 1;\n    }\n}\n");
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_style = space\nindent_size = 4\n");

        // StyleBro's analyzers (dotnet format checks the references of projects it runs analyzers on), and the package in the
        // restore output, as format asks.
        Write("Directory.Build.props", $"<Project>\n  <ItemGroup>\n    <Analyzer Include=\"{typeof(StyleBro.Analyzers.RepositoryNames).Assembly.Location}\" />\n  </ItemGroup>\n</Project>\n");
        foreach (var arguments in new[] { new[] { "new", "sln", "-n", "Chain", "--format", "sln" }, new[] { "sln", "Chain.sln", "add", "Lib1/Lib1.csproj", "Lib2/Lib2.csproj", "Tests/Tests.csproj" }, new[] { "restore", "Chain.sln", "-nodeReuse:false" } })
        {
            var step = new ProcessStartInfo("dotnet") { WorkingDirectory = root };
            arguments.ToList().ForEach(step.ArgumentList.Add);
            using var process = Process.Start(step)!;
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }

        foreach (var assets in Directory.GetFiles(root, "project.assets.json", SearchOption.AllDirectories))
        {
            var json = JsonNode.Parse(File.ReadAllText(assets))!;
            json["libraries"]!["StyleBro.Analyzers/0.0.0"] = new JsonObject { ["type"] = "package" };
            File.WriteAllText(assets, json.ToJsonString());
        }

        var lines = new List<string>();
        FormatCommand.Run([Path.Combine(root, "Chain.sln"), "--verify-no-changes", "--no-restore"], lines.Add);

        Assert.DoesNotContain(lines, l => l.Contains("skipped Tests", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.Contains("C.cs(5,20)", StringComparison.Ordinal));
    }

    [Fact]
    public void Preview_TheCopy_KeepsFilesCommittedWithCrLf()
    {
        // esprima-dotnet: '* text=auto', and two files committed with CRLF before it. The copy's repository stored them as LF,
        // so init counted them as converted by git (no 'end_of_line = crlf' for a file whose multi-line strings would change)
        // and the patch made them LF; in the repository init and format keep them CRLF.
        var source = Path.Combine(root, "source");
        Directory.CreateDirectory(source);
        void Git(string folder, params string[] arguments) => Assert.Equal(0, PreviewCommand.Git(folder, ["-c", "user.name=t", "-c", "user.email=t@example.invalid", .. arguments]).Code);
        Git(source, "init", "-q");
        File.WriteAllText(Path.Combine(source, "Crlf.cs"), "class Crlf\r\n{\r\n    string S = @\"a\r\nb\";\r\n}\r\n");
        File.WriteAllText(Path.Combine(source, "Lf.cs"), "class Lf\n{\n}\n");
        Git(source, "add", "-A");
        Git(source, "commit", "-q", "-m", "files");
        File.WriteAllText(Path.Combine(source, ".gitattributes"), "* text=auto\n");
        Git(source, "add", "-A");
        Git(source, "commit", "-q", "-m", "attributes");
        Assert.Equal(["Crlf.cs"], PreviewCommand.CommittedWithCrLf(PreviewCommand.Git(source, "ls-files", "--eol", "-z").Output));

        var copy = Path.Combine(root, "copy");
        PreviewCommand.MakeRepository(copy, PreviewCommand.Copy(source, copy), source: source);

        var eol = PreviewCommand.Git(copy, "ls-files", "--eol", "-z").Output.Split('\0');
        Assert.Contains(eol, e => e.StartsWith("i/crlf", StringComparison.Ordinal) && e.EndsWith("\tCrlf.cs", StringComparison.Ordinal));
        Assert.Contains(eol, e => e.StartsWith("i/lf", StringComparison.Ordinal) && e.EndsWith("\tLf.cs", StringComparison.Ordinal));
        Assert.Empty(PreviewCommand.CommittedWithCrLf("i/crlf  w/crlf  attr/text=auto \tmy file.cs\0i/lf    w/lf    attr/text=auto \tA.cs\0"));

        // The preview's own copy: init sees the CRLF file as the repository has it and keeps its ending.
        File.WriteAllText(Path.Combine(source, "App.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        for (var i = 0; i < 10; i++)
        {
            File.WriteAllText(Path.Combine(source, $"Lf{i}.cs"), $"class Lf{i}\n{{\n}}\n");
        }

        Git(source, "add", "-A");
        Git(source, "commit", "-q", "-m", "more");
        var patch = Path.Combine(root, "out.patch");
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

        Capture(() => Assert.Equal(0, PreviewCommand.Run("init", [source, "--diff=" + patch], Format)));
        Assert.Contains("[Crlf.cs]", File.ReadAllText(patch), StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_OfAProject_AddsThePackageToTheDirectoryBuildPropsItImports()
    {
        // AsyncEx: 'bro preview format src/Nito.AsyncEx.Tasks/Nito.AsyncEx.Tasks.csproj' created
        // src/Nito.AsyncEx.Tasks/Directory.Build.props, which hid src/Directory.Build.props (warnings as errors, docs, FxCop).
        Write("src/Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>\n  </PropertyGroup>\n</Project>\n");
        Write("src/Tasks/Tasks.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("test/Tests/Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        var tasks = Path.Combine(root, "src", "Tasks");

        Assert.Equal("StyleBro.Analyzers " + PackageVersionOfTool() + " to src/Directory.Build.props", PreviewCommand.AddPackage(tasks, root));
        Assert.Contains("StyleBro.Analyzers", File.ReadAllText(Path.Combine(root, "src", "Directory.Build.props")), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(tasks, "Directory.Build.props")));
        Assert.False(File.Exists(Path.Combine(root, "test", "Directory.Build.props")));

        // Central package management from above the project: the version goes into that Directory.Packages.props.
        Write("Directory.Packages.props", "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n  <ItemGroup>\n  </ItemGroup>\n</Project>\n");
        Write("lib/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Assert.NotNull(PreviewCommand.AddPackage(Path.Combine(root, "lib", "Lib"), root));
        Assert.Contains("<PackageVersion Include=\"StyleBro.Analyzers\"", File.ReadAllText(Path.Combine(root, "Directory.Packages.props")), StringComparison.Ordinal);
        Assert.Contains("<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />", File.ReadAllText(Path.Combine(root, "Directory.Build.props")), StringComparison.Ordinal);
    }

    private static string PackageVersionOfTool() =>
        typeof(PreviewCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
}
