// Times analyzers on a folder of C# sources: the compiler's own per-analyzer execution time (what ReportAnalyzer shows),
// all analyzers together, single-threaded, median of the runs after a warm-up run. See README.md.
//
//   dotnet run -c Release --project scripts/benchmark -- <analyzer dll> <source folder> [runs] [preprocessor symbols]
//
// STYLEBRO_BENCH_ONLY=NameA,NameB times only those analyzers (to compare two builds of one analyzer with less noise).
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

var dll = Path.GetFullPath(args[0]);
var sources = args[1];
var runs = args.Length > 2 ? int.Parse(args[2]) : 5;
var symbols = args.Length > 3 ? args[3].Split(';', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();

var assembly = new AssemblyLoadContext("analyzers").LoadFromAssemblyPath(dll);
var analyzers = assembly.GetTypes()
    .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && t.GetCustomAttribute<DiagnosticAnalyzerAttribute>() is not null)
    .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
    .Where(a => a is not DiagnosticSuppressor)
    .Where(a => Environment.GetEnvironmentVariable("STYLEBRO_BENCH_ONLY") is not { } only || only.Split(',').Contains(a.GetType().Name))
    .ToImmutableArray();

var parse = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose, preprocessorSymbols: symbols);
var separator = Path.DirectorySeparatorChar;
var trees = Directory.GetFiles(sources, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains($"{separator}obj{separator}") && !f.Contains($"{separator}bin{separator}"))
    .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), parse, f))
    .ToImmutableArray();
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p));
var compilation = CSharpCompilation.Create(
    "Benchmark", trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
Console.WriteLine($"{trees.Length} files, {analyzers.Length} analyzers, {runs} runs");
compilation.GetDiagnostics();

var times = analyzers.ToDictionary(a => a, _ => new List<double>());
var counts = new Dictionary<string, int>();
for (var i = 0; i <= runs; i++)
{
    var options = new CompilationWithAnalyzersOptions(
        new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty), null, concurrentAnalysis: false, logAnalyzerExecutionTime: true);
    var withAnalyzers = compilation.WithAnalyzers(analyzers, options);
    var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    if (i == 0)
    {
        counts = diagnostics.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.Count());
        continue;
    }

    foreach (var analyzer in analyzers)
    {
        times[analyzer].Add((await withAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, CancellationToken.None)).ExecutionTime.TotalMilliseconds);
    }
}

var results = analyzers
    .Select(a => (Name: a.GetType().Name, Ms: times[a].OrderBy(t => t).ElementAt(runs / 2), Diagnostics: a.SupportedDiagnostics.Sum(d => counts.GetValueOrDefault(d.Id))))
    .OrderByDescending(r => r.Ms)
    .ToList();
Console.WriteLine("     Time  Reports  Analyzer");
foreach (var (name, ms, reports) in results)
{
    Console.WriteLine($"{ms,7:N1} ms  {reports,7}  {name}");
}

Console.WriteLine($"{results.Sum(r => r.Ms),7:N1} ms           total");
