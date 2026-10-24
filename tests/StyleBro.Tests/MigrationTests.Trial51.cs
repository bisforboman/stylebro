using System.Diagnostics;
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
}
