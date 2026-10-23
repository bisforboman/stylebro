using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Host;

/// <summary>
/// The diagnostics each analyzer reported on a compilation, kept for as long as the compilation lives (docs/format-engine.md,
/// "Re-checks in the fix loop"). Before each id, dotnet format runs that id's analyzers again over every project that had it;
/// the host asks this cache, which runs an analyzer only on a compilation it hasn't seen it on: a project whose documents a
/// fix changed, or one that references such a project (Roslyn gives both a new compilation). The rest keep their
/// compilation, so their results are reused. An analyzer's diagnostics don't depend on which other analyzers run with it
/// (no suppressor runs here: dotnet format's per-id step leaves them out too), so each one is kept on its own.
/// </summary>
public sealed class AnalysisCache
{
    private readonly ConditionalWeakTable<Compilation, Results> results = new();

    /// <summary>How many times an analyzer ran on a compilation (tests: what the cache saved).</summary>
    public int Runs { get; private set; }

    /// <summary>Diagnostics in a fixed order: by file, position, id and message.</summary>
    public static ImmutableArray<Diagnostic> Ordered(IEnumerable<Diagnostic> diagnostics) =>
        [.. diagnostics.OrderBy(d => d.Location.SourceTree?.FilePath ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(d => d.Location.SourceSpan.Start)
            .ThenBy(d => d.Location.SourceSpan.End)
            .ThenBy(d => d.Id, StringComparer.Ordinal)
            .ThenBy(d => d.GetMessage(), StringComparer.Ordinal)];

    /// <summary>
    /// The diagnostics <paramref name="analyzers"/> report on <paramref name="compilation"/> with <paramref name="options"/>
    /// (pragma-suppressed ones left out, like CompilationWithAnalyzers.GetAnalyzerDiagnosticsAsync), in a fixed order: by
    /// file, position and id. <paramref name="running"/> is called with the number of analyzers that have to run, when any do.
    /// </summary>
    public async Task<ImmutableArray<Diagnostic>> GetAsync(Compilation compilation, AnalyzerOptions options, ImmutableArray<DiagnosticAnalyzer> analyzers, Action<int>? running, CancellationToken cancellation)
    {
        var cached = this.results.GetValue(compilation, _ => new Results());
        List<DiagnosticAnalyzer> missing;
        lock (cached)
        {
            if (!ReferenceEquals(cached.Options, options))
            {
                cached.Options = options;
                cached.ByAnalyzer.Clear();
            }

            missing = analyzers.Where(a => !cached.ByAnalyzer.ContainsKey(a)).Distinct().ToList();
        }

        if (missing.Count > 0)
        {
            running?.Invoke(missing.Count);
            var analysis = compilation.WithAnalyzers([.. missing], new CompilationWithAnalyzersOptions(options, null, concurrentAnalysis: true, logAnalyzerExecutionTime: false, reportSuppressedDiagnostics: false));
            var result = await analysis.GetAnalysisResultAsync(cancellation).ConfigureAwait(false);
            lock (cached)
            {
                foreach (var analyzer in missing)
                {
                    cached.ByAnalyzer[analyzer] = result.GetAllDiagnostics(analyzer);
                }

                this.Runs += missing.Count;
            }
        }

        lock (cached)
        {
            return Ordered(analyzers.Distinct().SelectMany(a => cached.ByAnalyzer[a]));
        }
    }

    private sealed class Results
    {
        public AnalyzerOptions? Options { get; set; }

        public Dictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> ByAnalyzer { get; } = [];
    }
}
