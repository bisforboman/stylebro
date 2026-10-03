# Analyzer benchmark

Times analyzers the way the compiler's `ReportAnalyzer` does (each analyzer's own execution time), but without the
noise of a full build: one compilation of a folder of sources, all analyzers together, single-threaded, the median of
several runs after a warm-up run.

```bash
dotnet build src/StyleBro.Analyzers
dotnet run -c Release --project scripts/benchmark -- src/StyleBro.Analyzers/bin/Debug/netstandard2.0/StyleBro.Analyzers.dll path/to/sources 5
```

The optional fourth argument sets preprocessor symbols (`HAVE_ASYNC;NET8_0`). Missing references don't matter much:
the sources are compiled against the .NET runtime only, so code using other packages binds partly. Compare two builds
of the analyzers on the same sources rather than reading the absolute numbers.

## Results (2026-10-02, Newtonsoft.Json's main project, 242 files)

| | All analyzers | Slowest |
|---|---|---|
| StyleBro 0.1.0-alpha.8 | 1,096 ms | FieldNamingAnalyzer 468 ms |
| StyleBro after caching type facts in FieldNamingAnalyzer | ~800 ms | DocumentationAnalyzer ~160 ms, FieldNamingAnalyzer ~150 ms |
| StyleCop.Analyzers 1.2.0-beta.556 (182 analyzers) | 1,671 ms | SA1121 152 ms, SA1101 108 ms |
| StyleBro, 2026-10-03 (88 rules, 46 analyzers) | 1,082-1,350 ms (two runs) | DocumentationAnalyzer ~180-210 ms, FieldNamingAnalyzer ~140-200 ms, CommentTextAnalyzer ~115-200 ms, BlankLineRunsAnalyzer ~110-120 ms |

Single runs vary by about 20% (the two 2026-10-03 runs of the same build differ by 25%). In real builds the analyzers run concurrently with each other and with the compiler,
so the wall-clock cost is smaller: the private 30-project app built in the same time with and without StyleBro.

2026-10-03: DirectiveSpacingAnalyzer walked every token (trivia included) of every file; it now walks only the
directives and skips files without any (45 -> 23 ms). The blank-line analyzers each walk all tokens; merging them into
one pass is the next step if analyzer time ever matters.
