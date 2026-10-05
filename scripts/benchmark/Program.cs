// Times analyzers on a folder of C# sources: the compiler's own per-analyzer execution time (what ReportAnalyzer shows),
// all analyzers together, single-threaded, median of the runs after a warm-up run. See README.md.
//
//   dotnet run -c Release --project scripts/benchmark -- <analyzer dll> <source folder> [runs] [preprocessor symbols]
//   dotnet run -c Release --project scripts/benchmark -- compare <base dll> <head dll> <source folder> [runs]
//
// compare runs both builds in this process on the same compilation, alternating, so machine noise hits both alike, and
// exits with 1 when the head build is clearly slower (the CI performance check).
//
// STYLEBRO_BENCH_ONLY=NameA,NameB times only those analyzers (to compare two builds of one analyzer with less noise).
//
//   dotnet run -c Release --project scripts/benchmark -- file <analyzer dll> <source folder> [N | file;file...] [runs]
//
// file times each analyzer on ONE document, as the IDE analyzes the open file on every edit: syntax and semantic
// diagnostics of that tree only (the N largest files, default 10, or the given files, which may lie outside the folder).
// --all-rules (any mode) also turns on the rules that are off by default.
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var allRules = args.Contains("--all-rules");
args = args.Where(a => a != "--all-rules").ToArray();
var mode = args[0] is "compare" or "file" ? args[0] : "";
var compare = mode == "compare";
var dlls = compare ? new[] { args[1], args[2] } : new[] { args[mode == "" ? 0 : 1] };
var rest = args.Skip(dlls.Length + (mode == "" ? 0 : 1)).ToArray();
var sources = rest[0];
var fileTargets = mode == "file" ? (rest.Length > 1 ? rest[1] : "10") : "";
rest = mode == "file" ? rest.Where((_, i) => i != 1).ToArray() : rest;
var runs = rest.Length > 1 ? int.Parse(rest[1]) : 5;
var symbols = rest.Length > 2 ? rest[2].Split(';', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();

var builds = dlls.Select((dll, i) => Load(Path.GetFullPath(dll), $"build{i}")).ToArray();

var parse = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose, preprocessorSymbols: symbols);
var separator = Path.DirectorySeparatorChar;
var files = Directory.GetFiles(sources, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains($"{separator}obj{separator}") && !f.Contains($"{separator}bin{separator}"))
    .Select(f => (Path: f, Text: File.ReadAllText(f)))
    .ToArray();
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToArray();
// --all-rules: every rule a build supports at warning, also the ones that are off by default.
var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
    .WithSpecificDiagnosticOptions(allRules
        ? builds.SelectMany(b => b.Analyzers).SelectMany(a => a.SupportedDiagnostics).Where(d => !d.IsEnabledByDefault)
            .Select(d => d.Id).Distinct().Select(id => KeyValuePair.Create(id, ReportDiagnostic.Warn))
        : Array.Empty<KeyValuePair<string, ReportDiagnostic>>());
Console.WriteLine($"{files.Length} files, {string.Join(" / ", builds.Select(b => b.Analyzers.Length))} analyzers, {runs} runs");
if (mode == "file")
{
    await PerFile(builds[0]);
    return 0;
}

// Run 0 is the warm-up (and gives the diagnostic counts).
await Measure(0, runs);

if (!compare)
{
    var build = builds[0];
    Console.WriteLine("     Time  Reports  Analyzer");
    foreach (var name in build.Times.Keys.OrderByDescending(build.Median))
    {
        Console.WriteLine($"{build.Median(name),7:N1} ms  {build.Reports(name),7}  {name}");
    }

    Console.WriteLine($"{build.Total(),7:N1} ms           total");
    return 0;
}

// Each analyzer's fastest run: other work on the machine only ever adds time, so more runs only bring a build closer to
// its real speed. A slowdown found after the first round is measured again (up to two more rounds): noise goes away,
// a real regression stays. Thresholds for "clearly slower": the total more than 10 % and 25 ms slower, one analyzer more
// than 50 % and 15 ms slower, or a new analyzer above 60 ms. ponytail: fixed thresholds, tune them if the check flaps.
var (baseBuild, head) = (builds[0], builds[1]);
var (problems, lines) = Evaluate();
for (var round = 1; round < 3 && problems.Count > 0; round++)
{
    Console.WriteLine($"Slower after round {round}: {string.Join("; ", problems)}. Measuring again.");
    await Measure(round * runs + 1, (round + 1) * runs);
    (problems, lines) = Evaluate();
}

var report = string.Join('\n', new[] { "## Analyzer performance (Newtonsoft.Json, base vs head, alternated, fastest run each)", "" }
    .Concat(lines)
    .Concat(problems.Count == 0 ? new[] { "", "No regression." } : new[] { "", "**Slower:**", "" }.Concat(problems.Select(p => "- " + p))));
Console.WriteLine(report);
if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } summary)
{
    File.AppendAllText(summary, report + "\n");
}

return problems.Count == 0 ? 0 : 1;

// The builds take turns in every run, and which goes first alternates too (the second build of a run measured 3-25 ms
// slower on unchanged analyzers); a collection before each keeps one build from paying for the other's garbage.
async Task Measure(int first, int last)
{
    for (var i = first; i <= last; i++)
    {
        foreach (var build in i % 2 == 0 ? builds : builds.Reverse())
        {
            // Freshly parsed sources for every run of every build, as a build sees them: nothing a previous run built
            // (red nodes, structured trivia, a cache an analyzer keeps per tree) carries over. Binding isn't timed.
            var compilation = CSharpCompilation.Create(
                "Benchmark",
                files.Select(f => CSharpSyntaxTree.ParseText(f.Text, parse, f.Path)),
                references,
                compilationOptions);
            compilation.GetDiagnostics();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var options = new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty), null, concurrentAnalysis: false, logAnalyzerExecutionTime: true);
            var withAnalyzers = compilation.WithAnalyzers(build.Analyzers, options);
            var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
            if (i == 0)
            {
                build.Counts = diagnostics.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.Count());
                continue;
            }

            foreach (var analyzer in build.Analyzers)
            {
                build.Times[analyzer.GetType().Name].Add((await withAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, CancellationToken.None)).ExecutionTime.TotalMilliseconds);
            }
        }
    }
}

// Per file: in every run each target gets a fresh tree (the other files are parsed once: one document's analysis doesn't
// walk them) in a new compilation. Its semantic model is bound first (not timed: the IDE has usually bound it), then a new
// CompilationWithAnalyzers (its telemetry adds up) runs the syntax and semantic analysis of that tree only: tree, node
// and semantic model actions, and the symbol actions of the symbols declared there. The runs go round the files, so
// machine noise spreads over all of them; each analyzer's fastest run counts (other work only adds time).
async Task PerFile(Build build)
{
    var targets = int.TryParse(fileTargets, out var count)
        ? files.OrderByDescending(f => f.Text.Length).Take(count).Select(f => f.Path).ToArray()
        : fileTargets.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(Path.GetFullPath).ToArray();
    files = files.Concat(targets.Where(t => files.All(f => f.Path != t)).Select(t => (t, File.ReadAllText(t)))).ToArray();
    var trees = files.ToDictionary(f => f.Path, f => CSharpSyntaxTree.ParseText(f.Text, parse, f.Path));
    var times = targets.ToDictionary(t => t, _ => build.Analyzers.ToDictionary(a => a.GetType().Name, _ => new List<double>()));
    var reports = targets.ToDictionary(t => t, _ => new Dictionary<string, int>());
    for (var run = 0; run <= runs; run++)
    {
        foreach (var target in targets)
        {
            var tree = CSharpSyntaxTree.ParseText(files.First(f => f.Path == target).Text, parse, target);
            var compilation = CSharpCompilation.Create("Benchmark", trees.Values.Where(t => t.FilePath != target).Append(tree), references, compilationOptions);
            var options = new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty), null, concurrentAnalysis: false, logAnalyzerExecutionTime: true);
            var withAnalyzers = compilation.WithAnalyzers(build.Analyzers, options);
            var model = withAnalyzers.Compilation.GetSemanticModel(tree);
            model.GetDiagnostics();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var diagnostics = (await withAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(tree, CancellationToken.None))
                .AddRange(await withAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(model, null, CancellationToken.None));
            foreach (var analyzer in build.Analyzers)
            {
                var name = analyzer.GetType().Name;
                if (run == 0)
                {
                    reports[target][name] = diagnostics.Count(d => analyzer.SupportedDiagnostics.Any(s => s.Id == d.Id));
                }
                else
                {
                    times[target][name].Add((await withAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, CancellationToken.None)).ExecutionTime.TotalMilliseconds);
                }
            }
        }
    }

    static double Fastest(List<double> t) => t.Min();
    var root = Path.GetFullPath(sources);
    string Name(string path) => path.StartsWith(root) ? Path.GetRelativePath(root, path) : Path.GetFileName(path);
    double Kb(string path) => files.First(f => f.Path == path).Text.Length / 1024.0;
    foreach (var target in targets)
    {
        var fastest = times[target].ToDictionary(p => p.Key, p => Fastest(p.Value));
        Console.WriteLine();
        Console.WriteLine($"{Name(target)}: {Kb(target):N0} KB, {fastest.Values.Sum():N1} ms, {reports[target].Values.Sum()} reports");
        foreach (var (name, ms) in fastest.OrderByDescending(p => p.Value).Where(p => p.Value >= 0.5).Take(8))
        {
            Console.WriteLine($"{ms,9:N1} ms  {reports[target][name],6}  {name}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Slowest analyzers on one file (fastest run, and microseconds per KB of that file):");
    var worst = build.Analyzers.Select(a => a.GetType().Name)
        .Select(name => targets.Select(t => (Name: name, File: t, Ms: Fastest(times[t][name]))).MaxBy(x => x.Ms))
        .OrderByDescending(x => x.Ms).Take(15);
    foreach (var (name, file, ms) in worst)
    {
        Console.WriteLine($"{ms,9:N1} ms  {ms * 1000 / Kb(file),6:N0} us/KB  {name}  ({Name(file)})");
    }

    Console.WriteLine($"{targets.Average(t => times[t].Values.Sum(Fastest)),9:N1} ms  average total per file");
    if (Environment.GetEnvironmentVariable("STYLEBRO_BENCH_CSV") is { Length: > 0 } csv)
    {
        File.WriteAllLines(csv, targets.SelectMany(t => times[t].Select(p => $"{Name(t)},{Kb(t):F0},{p.Key},{Fastest(p.Value):F2},{reports[t][p.Key]}")).Prepend("file,kb,analyzer,ms,reports"));
    }

    Console.WriteLine();
    Console.WriteLine("Reports (all files): " + string.Join(", ", build.Analyzers.Select(a => a.GetType().Name).OrderBy(n => n)
        .Select(n => (Name: n, Count: targets.Sum(t => reports[t][n]))).Where(x => x.Count > 0).Select(x => $"{x.Name} {x.Count}")));
}

(List<string> Problems, List<string> Lines) Evaluate()
{
    var problems = new List<string>();
    var lines = new List<string> { "| Analyzer | Base | Head | Change | Reports (base / head) |", "|---|---:|---:|---:|---:|" };
    foreach (var name in head.Times.Keys.Union(baseBuild.Times.Keys).OrderByDescending(n => Math.Max(head.Fastest(n), baseBuild.Fastest(n))))
    {
        var (b, h) = (baseBuild.Fastest(name), head.Fastest(name));
        var isNew = !baseBuild.Times.ContainsKey(name);
        var removed = !head.Times.ContainsKey(name);
        if (isNew && h > 60)
        {
            problems.Add($"{name} is new and takes {h:N1} ms");
        }
        else if (!isNew && !removed && h > b * 1.5 && h - b > 15)
        {
            problems.Add($"{name}: {b:N1} -> {h:N1} ms");
        }

        var change = isNew ? "new" : removed ? "removed" : Signed(h - b);
        lines.Add($"| {name} | {(isNew ? "" : $"{b:N1} ms")} | {(removed ? "" : $"{h:N1} ms")} | {change} | {baseBuild.Reports(name)} / {head.Reports(name)} |");
    }

    var (baseTotal, headTotal) = (baseBuild.Times.Keys.Sum(baseBuild.Fastest), head.Times.Keys.Sum(head.Fastest));
    lines.Add($"| **total** | **{baseTotal:N1} ms** | **{headTotal:N1} ms** | **{Signed(headTotal - baseTotal)}** | |");
    if (headTotal > baseTotal * 1.1 && headTotal - baseTotal > 25)
    {
        problems.Add($"total: {baseTotal:N1} -> {headTotal:N1} ms");
    }

    return (problems, lines);
}

static string Signed(double ms) => (Math.Round(ms, 1) is var r && r == 0 ? "+0.0" : r.ToString("+0.0;-0.0")) + " ms";

static Build Load(string dll, string name)
{
    var assembly = new AssemblyLoadContext(name).LoadFromAssemblyPath(dll);
    var analyzers = assembly.GetTypes()
        .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && t.GetCustomAttribute<DiagnosticAnalyzerAttribute>() is not null)
        .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
        .Where(a => a is not DiagnosticSuppressor)
        .Where(a => Environment.GetEnvironmentVariable("STYLEBRO_BENCH_ONLY") is not { } only || only.Split(',').Contains(a.GetType().Name))
        .ToImmutableArray();
    return new Build(analyzers);
}

sealed class Build(ImmutableArray<DiagnosticAnalyzer> analyzers)
{
    public ImmutableArray<DiagnosticAnalyzer> Analyzers { get; } = analyzers;

    public Dictionary<string, List<double>> Times { get; } = analyzers.ToDictionary(a => a.GetType().Name, _ => new List<double>());

    public Dictionary<string, int> Counts { get; set; } = new();

    public double Median(string name) => Times.TryGetValue(name, out var t) ? t.OrderBy(x => x).ElementAt(t.Count / 2) : 0;

    public double Fastest(string name) => Times.TryGetValue(name, out var t) ? t.Min() : 0;

    public double Total() => Times.Keys.Sum(Median);

    public int Reports(string name) =>
        Analyzers.FirstOrDefault(a => a.GetType().Name == name)?.SupportedDiagnostics.Sum(d => Counts.GetValueOrDefault(d.Id)) ?? 0;
}
