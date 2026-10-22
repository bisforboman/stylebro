// Times dotnet format's fix loop per diagnostic id (performance audit 2026-10, README.md "Fix loop probe").
//
// dotnet format fixes one id at a time: it runs the analyzers that support the id over every project with findings
// (AnalyzerRunner.RunCodeAnalysisAsync), then the id's Fix All (SolutionCodeFixApplier.ApplyCodeFixesAsync), but it
// doesn't log which id. Loaded with DOTNET_STARTUP_HOOKS, this patches those two methods (Harmony) and writes one
// tab-separated line per call to $FORMAT_PROBE_LOG:
//
//   seconds  analyze  <project>  <ms>  <diagnostics found>  <ids every given analyzer supports, ','>
//   seconds  fix      <id>       <ms>  <diagnostics fixed>  <documents changed>  <files changed>
//   seconds  phase    fixing
//
// Analysis lines before "phase fixing" are the first analysis; after it, they belong to the next "fix" line (unless
// the analysis found nothing, then dotnet format skips the id: the summary script checks the candidate ids).
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

internal static class StartupHook
{
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static readonly object Gate = new();
    private static StreamWriter? log;

    public static void Initialize()
    {
        var path = Environment.GetEnvironmentVariable("FORMAT_PROBE_LOG");
        if (string.IsNullOrEmpty(path) || Assembly.GetEntryAssembly()?.GetName().Name != "dotnet-format")
        {
            return;
        }

        var folder = Path.GetDirectoryName(typeof(StartupHook).Assembly.Location)!;
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var file = Path.Combine(folder, new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(file) && !args.Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
                ? Assembly.LoadFrom(file)
                : null;
        };
        log = new StreamWriter(path, append: true) { AutoFlush = true };
        Patch();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Patch()
    {
        var harmony = new Harmony("stylebro.formatprobe");
        var runner = AccessTools.TypeByName("Microsoft.CodeAnalysis.Tools.Analyzers.AnalyzerRunner");
        var analyze = runner.GetMethods().Single(m => m.Name == "RunCodeAnalysisAsync"
            && m.GetParameters()[1].ParameterType != typeof(DiagnosticAnalyzer));
        harmony.Patch(analyze, prefix: new HarmonyMethod(typeof(StartupHook), nameof(Start)),
            postfix: new HarmonyMethod(typeof(StartupHook), nameof(AnalyzeDone)));
        var applier = AccessTools.TypeByName("Microsoft.CodeAnalysis.Tools.Analyzers.SolutionCodeFixApplier");
        harmony.Patch(AccessTools.Method(applier, "ApplyCodeFixesAsync"), prefix: new HarmonyMethod(typeof(StartupHook), nameof(Start)),
            postfix: new HarmonyMethod(typeof(StartupHook), nameof(FixDone)));
        var formatter = AccessTools.TypeByName("Microsoft.CodeAnalysis.Tools.Analyzers.AnalyzerFormatter");
        harmony.Patch(AccessTools.Method(formatter, "FixDiagnosticsAsync"), prefix: new HarmonyMethod(typeof(StartupHook), nameof(Fixing)));
    }

    private static void Write(string line)
    {
        lock (Gate)
        {
            log!.WriteLine($"{Clock.Elapsed.TotalSeconds:F2}\t{line}");
        }
    }

    private static void Start(out long __state) => __state = Stopwatch.GetTimestamp();

    private static void Fixing() => Write("phase\tfixing");

    private static void AnalyzeDone(ref Task __result, long __state, object result, ImmutableArray<DiagnosticAnalyzer> analyzers, Project project)
    {
        var task = __result;
        var name = project.Name;
        var candidates = analyzers.Select(a => a.SupportedDiagnostics.Select(d => d.Id))
            .Aggregate((a, b) => a.Intersect(b)).Distinct().ToList();
        var ids = analyzers.Length > 20 ? "*" : string.Join(",", candidates);
        __result = task.ContinueWith(
            t =>
            {
                var ms = Stopwatch.GetElapsedTime(__state).TotalMilliseconds;
                Write($"analyze\t{name}\t{ms:F0}\t{Count(result)}\t{ids}");
                return t;
            },
            TaskScheduler.Default).Unwrap();
    }

    private static int Count(object result)
    {
        var diagnostics = (IEnumerable)result.GetType().GetProperty("Diagnostics")!.GetValue(result)!;
        var count = 0;
        foreach (var pair in diagnostics)
        {
            count += ((ICollection)pair.GetType().GetProperty("Value")!.GetValue(pair)!).Count;
        }

        return count;
    }

    private static void FixDone(ref Task<Solution> __result, long __state, Solution solution, object result, string diagnosticId)
    {
        var task = __result;
        var count = Count(result);
        __result = task.ContinueWith(
            t =>
            {
                var ms = Stopwatch.GetElapsedTime(__state).TotalMilliseconds;
                var documents = 0;
                var files = new HashSet<string>();
                if (t.IsCompletedSuccessfully)
                {
                    foreach (var change in t.Result.GetChanges(solution).GetProjectChanges())
                    {
                        foreach (var id in change.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
                        {
                            documents++;
                            files.Add(change.NewProject.GetDocument(id)?.FilePath ?? id.ToString());
                        }
                    }
                }

                Write($"fix\t{diagnosticId}\t{ms:F0}\t{count}\t{documents}\t{files.Count}");
                return t;
            },
            TaskScheduler.Default).Unwrap();
    }
}
