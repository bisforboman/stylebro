using System.Diagnostics;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the thirty-second trial (ControlzEx, Pinta, CAP) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Format_ATransitiveReferenceOfAProjectWithoutTheRunsFramework_AsksForAllItsFrameworks()
    {
        // CAP: Dashboard.K8s (net8.0) references Dashboard (net8.0), which references CAP (net8.0;net10.0). The SDK adds CAP
        // to K8s's references during the build (transitive project references), after the evaluation the hook's Update
        // marks: in the net10.0 run CAP offered only net10.0 to K8s ("It cannot be referenced by a project that targets
        // .NETCoreApp,Version=v8.0"), and the two samples that reference K8s were skipped.
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFrameworks>net8.0;net10.0</TargetFrameworks></PropertyGroup>\n</Project>\n");
        Write("Lib/Lib.cs", "namespace Lib;\n\npublic class A\n{\n}\n");
        Write("Mid/Mid.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n  <ItemGroup><ProjectReference Include=\"../Lib/Lib.csproj\" /></ItemGroup>\n</Project>\n");
        Write("Top/Top.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n  <ItemGroup><ProjectReference Include=\"../Mid/Mid.csproj\" /></ItemGroup>\n</Project>\n");
        Write("Top/Top.cs", "namespace Top;\n\npublic class B : Lib.A\n{\n}\n");
        string Full(string name) => Path.Combine(root, name, name + ".csproj");
        var projects = new Dictionary<string, (string[] Frameworks, string[] References)>(StringComparer.OrdinalIgnoreCase)
        {
            [Full("Lib")] = (new[] { "net8.0", "net10.0" }, Array.Empty<string>()),
            [Full("Mid")] = (new[] { "net8.0" }, new[] { Full("Lib") }),
            [Full("Top")] = (new[] { "net8.0" }, new[] { Full("Mid") }),
        };
        var keep = FormatCommand.Keep(projects, "net10.0");
        var other = FormatCommand.Other(projects, keep, "net10.0");
        Assert.Equal(new[] { Full("Mid"), Full("Top") }, other.Order(StringComparer.Ordinal));
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

        Dotnet(false, "restore", "Top/Top.csproj");
        Assert.Equal(0, Dotnet(true, "build", "Top/Top.csproj", "--no-restore"));
    }

    [Fact]
    public void Migrate_CaseBlocksAtTheLabelsLevel_AreKept()
    {
        // ControlzEx (StyleCop-clean): its 7 'case X:' blocks have '{' at the label's column, which no StyleCop rule checks,
        // and format's whitespace pass indented them all (owner's decision 2026-10-18: migrate follows the code there).
        Write("Lib.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write(".editorconfig", "root = true\n");
        Write("C.cs", """
            namespace Lib
            {
                public class C
                {
                    public int M(int x)
                    {
                        switch (x)
                        {
                            case 0:
                            {
                                return 0;
                            }

                            case 1:
                            {
                                return 1;
                            }

                            default:
                            {
                                return 2;
                            }
                        }
                    }
                }
            }
            """);

        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Contains("csharp_indent_case_contents_when_block = false", result.Lines);
        Assert.Contains(result.Notes, n => n.StartsWith("'case X:' blocks: StyleCop doesn't check their indentation, and your code has 0 indented below the label, 3 at the label's level", StringComparison.Ordinal));

        // The SDK's form in the code: nothing to write or say.
        Write("C.cs", "namespace Lib\n{\n    public class C\n    {\n        public int M(int x)\n        {\n            switch (x)\n            {\n                case 0:\n                    {\n                        return 0;\n                    }\n            }\n\n            return -1;\n        }\n    }\n}\n");
        result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("csharp_indent_case_contents_when_block", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Notes, n => n.StartsWith("'case X:' blocks", StringComparison.Ordinal));
    }
}
