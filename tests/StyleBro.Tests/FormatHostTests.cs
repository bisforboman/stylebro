using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Host;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>
/// The in-process engine of 'bro format' (--engine host, docs/proposals/format-speed.md, phase 1): reading 'dotnet format''s
/// command line, the views of a workspace a plan entry checks, loading the SDK's Roslyn next to the tool's, and, against the
/// SDK on this machine, that a check in the host says what 'dotnet format' says.
/// </summary>
[Collection(FormatEnvironmentCollection.Name)]
public sealed class FormatHostTests : IDisposable
{
    private readonly List<string> folders = [];

    [Fact]
    public void Arguments_AreReadLikeDotnetFormatReadsThem()
    {
        var parsed = FormatArguments.Parse(["style", "/r/.stylebro-format-0.slnf", "--no-restore", "--diagnostics", "BRO1001", "IDE0011", "--severity", "warn", "--verify-no-changes", "--include", "src/A/", "src/B/", "--exclude", "**/*.vb", "--report", "/tmp/r"]);

        Assert.NotNull(parsed);
        Assert.Equal("style", parsed.Pass);
        Assert.Equal("/r/.stylebro-format-0.slnf", parsed.Target);
        Assert.True(parsed.IsSolution);
        Assert.True(parsed.Verify);
        Assert.True(parsed.NoRestore);
        Assert.Equal(["BRO1001", "IDE0011"], parsed.Diagnostics);
        Assert.Equal(["src/A/", "src/B/"], parsed.Include);
        Assert.Equal(["**/*.vb"], parsed.Exclude);
        Assert.Equal("warn", parsed.Severity);
        Assert.Equal("/tmp/r", parsed.Report);
        Assert.Equal(2, FormatArguments.Categories(parsed.Pass));
        Assert.Equal(7, FormatArguments.Categories(FormatArguments.Parse(["/r/App.csproj"])!.Pass));
        Assert.False(FormatArguments.Parse(["/r/App.csproj"])!.IsSolution);
    }

    [Theory]
    [InlineData("--folder")]
    [InlineData("--binarylog")]
    [InlineData("--severity", "fatal")]
    [InlineData("--verbosity", "loud")]
    public void Arguments_TheHostDoesntRun_AreDeclined(params string[] option)
    {
        Assert.Null(FormatArguments.Parse(["/r/App.slnx", "--verify-no-changes", .. option]));
        Assert.Null(FormatArguments.Parse(["--verify-no-changes"]));
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData("q", 4)]
    [InlineData("normal", 2)]
    [InlineData("diag", 0)]
    public void Verbosity_IsDotnetFormatsLogLevel(string? verbosity, int level) => Assert.Equal(level, FormatArguments.LogLevelOf(verbosity));

    [Theory]
    [InlineData("Lib(net8.0)", "net8.0")]
    [InlineData("Lib(net10.0-windows)", "net10.0-windows")]
    [InlineData("Lib", null)]
    [InlineData("(x", null)]
    public void FrameworkOf_ReadsTheInstanceName(string name, string? framework) => Assert.Equal(framework, Views.FrameworkOf(name));

    [Fact]
    public void AView_HasTheRunsInstances_AndWhatTheyReference()
    {
        using var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution;
        ProjectId Add(string name, string path, params ProjectId[] references)
        {
            var id = ProjectId.CreateNewId(name);
            solution = solution.AddProject(ProjectInfo.Create(id, VersionStamp.Default, name, name, LanguageNames.CSharp, filePath: path, projectReferences: references.Select(r => new ProjectReference(r))));
            return id;
        }

        var lib8 = Add("Lib(net8.0)", "/r/Lib/Lib.csproj");
        var lib10 = Add("Lib(net10.0)", "/r/Lib/Lib.csproj");
        var core = Add("Core", "/r/Core/Core.csproj");
        var app = Add("App", "/r/App/App.csproj", lib10, core);
        var other = Add("Other", "/r/Other/Other.csproj");

        var names = new Dictionary<string, string>();
        var view = Views.Of(solution, ["/r/App/App.csproj|", "/r/Lib/Lib.csproj|net10.0"], names);

        Assert.Equal(new HashSet<ProjectId> { lib10, core, app }, view.ProjectIds.ToHashSet());
        Assert.Equal(new Dictionary<string, string> { ["Lib(net10.0)"] = "Lib" }, names);

        names.Clear();
        var net8 = Views.Of(solution, ["/r/Lib/Lib.csproj|net8.0"], names);
        Assert.Equal([lib8], net8.ProjectIds);
        Assert.Same(solution, Views.Of(solution, [], names));
        Assert.Contains(other, solution.ProjectIds);
    }

    [Fact]
    public void AView_HasTheLoadWarnings_OfItsProjects_AndOfProjectsThatDidntLoad()
    {
        using var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "A", "A", LanguageNames.CSharp, filePath: "/r/A/A.csproj"))
            .AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "B", "B", LanguageNames.CSharp, filePath: "/r/B/B.csproj"));
        var view = Views.Of(solution, ["/r/A/A.csproj|"], []);
        var b = solution.Projects.Single(p => p.Name == "B").Id;
        WorkspaceDiagnostic Warning(string message) => new(WorkspaceDiagnosticKind.Warning, message);

        // The last one names A, but is about B (System.IO.Abstractions: it named the wrong run).
        var warnings = Views.WarningsOf(
            [Warning("Msbuild failed when processing the file '/r/A/A.csproj'"), Warning("Msbuild failed when processing the file '/r/B/B.csproj'"), Warning("Cannot open project '/r/F/F.fsproj'"), new ProjectDiagnostic(WorkspaceDiagnosticKind.Warning, "Found project reference without a matching metadata reference: /r/A/A.csproj", b)],
            solution,
            view);

        Assert.Equal(["Msbuild failed when processing the file '/r/A/A.csproj'", "Cannot open project '/r/F/F.fsproj'"], warnings.Select(w => w.Message));
        Assert.Single(Views.WarningsOf([new ProjectDiagnostic(WorkspaceDiagnosticKind.Warning, "Found project reference without a matching metadata reference: /r/A/A.csproj", b)], solution, solution));
    }

    // Serilog on Linux: its net462 instance doesn't load ('Serilog', no framework in the name), and the failure names the
    // project, whose other instances did. The net462 run, whose TestDummies(net462) gets Serilog(netstandard2.0), reports it,
    // as a 'dotnet format' run of net462 alone does; the net10.0 run, which has Serilog(net10.0), doesn't.
    [Fact]
    public void ALoadFailure_OfAnInstance_GoesToTheRunOfItsFramework()
    {
        using var workspace = new AdhocWorkspace();
        var standard = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "Lib(net10.0)", "Lib", LanguageNames.CSharp, filePath: "/r/Lib/Lib.csproj"))
            .AddProject(ProjectInfo.Create(standard, VersionStamp.Default, "Lib(netstandard2.0)", "Lib", LanguageNames.CSharp, filePath: "/r/Lib/Lib.csproj"))
            .AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "Lib", "Lib", LanguageNames.CSharp, filePath: "/r/Lib/Lib.csproj"))
            .AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "Dummies(net462)", "Dummies", LanguageNames.CSharp, filePath: "/r/Dummies/Dummies.csproj", projectReferences: [new ProjectReference(standard)]));
        var failure = new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Failure, "Msbuild failed when processing the file '/r/Lib/Lib.csproj' with message: Project does not contain 'Compile' target.");
        string[] net10 = ["/r/Lib/Lib.csproj|net10.0"];
        string[] net462 = ["/r/Dummies/Dummies.csproj|net462"];

        Assert.Equal(["net462"], Views.FrameworksOf(net462));
        Assert.Empty(Views.WarningsOf([failure], solution, Views.Of(solution, net10, []), Views.FrameworksOf(net10)));
        Assert.Single(Views.WarningsOf([failure], solution, Views.Of(solution, net462, []), Views.FrameworksOf(net462)));
    }

    [Fact]
    public void TheEngineOption_IsTakenOut()
    {
        string[] args = ["/r", "--engine", "host", "--verify-no-changes"];
        Assert.Equal(FormatCommand.HostEngine, FormatCommand.TakeEngine(ref args, out var error));
        Assert.Null(error);
        Assert.Equal(["/r", "--verify-no-changes"], args);

        args = ["/r"];
        Assert.Equal(FormatCommand.DotnetFormatEngine, FormatCommand.TakeEngine(ref args, out _));

        args = ["/r", "--engine", "fast"];
        Assert.Null(FormatCommand.TakeEngine(ref args, out error));
        Assert.Contains("--engine takes 'host'", error);
        Assert.Equal(1, FormatCommand.Run(["/r", "--engine"], _ => { }, (_, _) => throw new InvalidOperationException("ran")));
    }

    [Fact]
    public void TheSdkFolder_ComesFromListSdks()
    {
        const string list = "8.0.132 [/usr/lib/dotnet/sdk]\n10.0.113 [/usr/lib/dotnet/sdk]\n10.0.200 [/opt/dotnet/sdk]\n";

        Assert.Equal(Path.Combine("/usr/lib/dotnet/sdk", "10.0.113"), FormatHost.SdkFolder("10.0.113", list));
        Assert.Equal(Path.Combine("/opt/dotnet/sdk", "10.0.200"), FormatHost.SdkFolder("10.0.200", list));
        Assert.Null(FormatHost.SdkFolder("10.0.1", list));
    }

    [Fact]
    public void TheRuntime_ComesFromTheRuntimeConfig()
    {
        var root = this.NewFolder();
        var single = Path.Combine(root, "a.runtimeconfig.json");
        var many = Path.Combine(root, "b.runtimeconfig.json");
        File.WriteAllText(single, """{ "runtimeOptions": { "tfm": "net10.0", "framework": { "name": "Microsoft.NETCore.App", "version": "10.0.0" } } }""");
        File.WriteAllText(many, """{ "runtimeOptions": { "frameworks": [ { "name": "Microsoft.NETCore.App", "version": "11.0.0-preview.1" } ] } }""");

        Assert.Equal(10, FormatHost.RuntimeMajor(single));
        Assert.Null(FormatHost.RuntimeMajor(many)); // a prerelease version doesn't parse: no answer, the host tries
        Assert.Null(FormatHost.RuntimeMajor(Path.Combine(root, "missing.json")));
    }

    [Fact]
    public void TheNewestRuntime_IsTheHighestMajorNextToThisOne()
    {
        var shared = Path.Combine(this.NewFolder(), "Microsoft.NETCore.App");
        foreach (var version in new[] { "8.0.32", "10.0.13", "11.0.0-preview.2.1" })
        {
            Directory.CreateDirectory(Path.Combine(shared, version));
        }

        Assert.Equal(11, FormatHost.NewestRuntime(Path.Combine(shared, "8.0.32")));
        Assert.Null(FormatHost.NewestRuntime(null));
    }

    [Fact]
    public void TheCommandRunAgain_PutsTheRollForwardVariableBack()
    {
        Environment.SetEnvironmentVariable("STYLEBRO_ROLLED_FORWARD", "Minor");
        Environment.SetEnvironmentVariable("DOTNET_ROLL_FORWARD", "LatestMajor");
        try
        {
            Assert.Null(FormatHost.RollForward(["format", ".", "--engine", "host"]));
            Assert.Equal("Minor", Environment.GetEnvironmentVariable("DOTNET_ROLL_FORWARD"));
            Assert.Null(Environment.GetEnvironmentVariable("STYLEBRO_ROLLED_FORWARD"));
            Assert.Null(FormatHost.RollForward(["format", "."]));
        }
        finally
        {
            Environment.SetEnvironmentVariable("STYLEBRO_ROLLED_FORWARD", null);
            Environment.SetEnvironmentVariable("DOTNET_ROLL_FORWARD", null);
        }
    }

    [Fact]
    public void TheSdkContext_RefusesTheToolsOwnAssemblies()
    {
        var root = this.NewFolder();
        var sdk = Directory.CreateDirectory(Path.Combine(root, "sdk")).FullName;
        var tool = Directory.CreateDirectory(Path.Combine(root, "tool")).FullName;
        foreach (var file in new[] { Path.Combine(sdk, "Microsoft.CodeAnalysis.dll"), Path.Combine(tool, "Microsoft.CodeAnalysis.dll"), Path.Combine(tool, "StyleBro.Analyzers.dll") })
        {
            File.WriteAllText(file, string.Empty);
        }

        Assert.True(FormatHost.Refuses("StyleBro.Analyzers", sdk, tool));
        Assert.False(FormatHost.Refuses("Microsoft.CodeAnalysis", sdk, tool));
        Assert.False(FormatHost.Refuses("System.Runtime", sdk, tool));
    }

    // Against the SDK on this machine: the host's check of a multi-targeted solution says what 'dotnet format' says, and a
    // framework's view sees that framework's '#if' code only.
    [Fact]
    public void AHostCheck_SaysWhatDotnetFormatSays()
    {
        var root = this.NewFolder();
        Write(root, ".editorconfig", "root = true\n\n[*.cs]\ncsharp_prefer_braces = true\ndotnet_diagnostic.IDE0011.severity = warning\n");
        Write(root, "App.slnx", "<Solution>\n  <Project Path=\"Lib/Lib.csproj\" />\n  <Project Path=\"App/App.csproj\" />\n</Solution>\n");
        Write(root, "Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>\n  </PropertyGroup>\n</Project>\n");
        Write(root, "Lib/C.cs", "namespace Lib;\n\npublic static class C\n{\n    public static int M(int x)\n    {\n#if NET10_0_OR_GREATER\n        if (x > 1) return 2;\n#endif\n        return x;\n    }\n}\n");
        Write(root, "App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <OutputType>Exe</OutputType>\n    <TargetFramework>net10.0</TargetFramework>\n  </PropertyGroup>\n  <ItemGroup>\n    <ProjectReference Include=\"../Lib/Lib.csproj\" />\n  </ItemGroup>\n</Project>\n");
        Write(root, "App/P.cs", "if (args.Length > 3) return 1;\nreturn Lib.C.M(args.Length);\n");
        Assert.Equal(0, Dotnet(root, out _, "restore", "App.slnx"));

        var solution = Path.Combine(root, "App.slnx");
        string[] check = ["format", "style", solution, "--verify-no-changes", "--no-restore", "--diagnostics", "IDE0011", "--severity", "warn"];
        var plainCode = Dotnet(root, out var plain, check);
        var log = new List<string>();
        using var host = FormatHost.Open(root, log.Add);
        Assert.True(host is not null, string.Join("\n", log));

        var whole = new List<string>();
        var wholeCode = host.Run(solution, root, [], [], [], check, whole.Add);
        Assert.Equal(2, plainCode);
        Assert.Equal(plainCode, wholeCode);
        Assert.Equal(plain.Where(FormatCommand.IsFindingLine).Order(), whole.Where(FormatCommand.IsFindingLine).Order());
        Assert.Equal(2, whole.Count(FormatCommand.IsFindingLine));

        var net8 = new List<string>();
        Assert.Equal(0, host.Run(solution, root, [], [], [$"{Path.Combine(root, "Lib", "Lib.csproj")}|net8.0"], check, net8.Add));
        Assert.DoesNotContain(net8, FormatCommand.IsFindingLine);

        var net10 = new List<string>();
        Assert.Equal(2, host.Run(solution, root, [], [], [$"{Path.Combine(root, "Lib", "Lib.csproj")}|net10.0"], check, net10.Add));
        Assert.Single(net10, l => FormatCommand.IsFindingLine(l) && l.Contains("C.cs(8,", StringComparison.Ordinal));

        // A command it doesn't run goes back to 'dotnet format'.
        Assert.Null(host.Run(solution, root, [], [], [], ["format", solution, "--no-restore", "--binarylog", "x.binlog"], _ => { }));
    }

    // Against the SDK on this machine: a fix run in the host writes what 'dotnet format' writes, the next run of the same host
    // sees it (and what else changed on disk), and a framework's fix reaches the other frameworks' copies of the file.
    [Fact]
    public void AHostFix_WritesWhatDotnetFormatWrites()
    {
        var root = this.NewFolder();
        Write(root, ".editorconfig", "root = true\n\n[*.cs]\ncsharp_prefer_braces = true\ndotnet_diagnostic.IDE0011.severity = warning\n");
        Write(root, "App.slnx", "<Solution>\n  <Project Path=\"Lib/Lib.csproj\" />\n</Solution>\n");
        Write(root, "Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>\n  </PropertyGroup>\n</Project>\n");
        Write(root, "Lib/C.cs", "namespace Lib;\n\npublic static class C\n{\n    public static int M(int x)\n    {\n        if (x > 2) return 3;\n#if NET10_0_OR_GREATER\n        if (x > 1) return 2;\n#endif\n        return x;\n    }\n}\n");
        Write(root, "Lib/D.cs", "namespace Lib;\n\npublic static class D\n{\n    public static int M(int x)\n    {\n        return x;\n    }\n}\n");
        Assert.Equal(0, Dotnet(root, out _, "restore", "App.slnx"));
        var copy = this.NewFolder();
        foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            Write(copy, Path.GetRelativePath(root, file), File.ReadAllText(file));
        }

        // 'dotnet format' on the copy, one framework at a time as bro runs it.
        string[] Fix(string folder) => ["format", "style", Path.Combine(folder, "App.slnx"), "--no-restore", "--diagnostics", "IDE0011", "--severity", "warn"];
        Assert.Equal(0, Dotnet(copy, out _, Fix(copy)));
        var expected = File.ReadAllText(Path.Combine(copy, "Lib", "C.cs"));
        Assert.Contains("{", expected.Split('\n')[6 + 1], StringComparison.Ordinal);

        var log = new List<string>();
        using var host = FormatHost.Open(root, log.Add);
        Assert.True(host is not null, string.Join("\n", log));
        var solution = Path.Combine(root, "App.slnx");
        var lib = Path.Combine(root, "Lib", "Lib.csproj");
        Assert.Equal(0, host.Run(solution, root, [], [], [$"{lib}|net8.0"], Fix(root), _ => { }));
        Assert.Equal(0, host.Run(solution, root, [], [], [$"{lib}|net10.0"], Fix(root), _ => { }));
        Assert.Equal(expected, File.ReadAllText(Path.Combine(root, "Lib", "C.cs")));

        // The net8.0 copy has the net10.0 run's text: neither framework finds anything now.
        string[] check = [.. Fix(root), "--verify-no-changes"];
        Assert.Equal(0, host.Run(solution, root, [], [], [$"{lib}|net8.0"], check, _ => { }));
        Assert.Equal(0, host.Run(solution, root, [], [], [$"{lib}|net10.0"], check, _ => { }));

        // A file changed on disk between runs (by bro, CSharpier, ...) is what the next run sees.
        File.WriteAllText(Path.Combine(root, "Lib", "D.cs"), "namespace Lib;\n\npublic static class D\n{\n    public static int M(int x)\n    {\n        if (x > 2) return 3;\n        return x;\n    }\n}\n");
        var found = new List<string>();
        Assert.Equal(2, host.Run(solution, root, [], [], [$"{lib}|net8.0"], check, found.Add));
        Assert.Single(found, l => FormatCommand.IsFindingLine(l) && l.Contains("D.cs(7,", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        foreach (var folder in this.folders)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static void Write(string root, string path, string text)
    {
        var full = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    private static int Dotnet(string folder, out List<string> output, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = folder, RedirectStandardOutput = true, RedirectStandardError = true };
        arguments.ToList().ForEach(start.ArgumentList.Add);

        // A build node that outlives the restore would keep the output open (and this read waiting).
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        start.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        using var process = Process.Start(start)!;
        var errors = process.StandardError.ReadToEndAsync();
        output = process.StandardOutput.ReadToEnd().Split('\n').Concat(errors.Result.Split('\n')).Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToList();
        process.WaitForExit();
        return process.ExitCode;
    }

    private string NewFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"stylebro-host-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        this.folders.Add(folder);
        return folder;
    }
}
