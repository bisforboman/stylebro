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
| StyleBro, 2026-10-04 (55 analyzers) | 806-858 ms (three runs) | DocumentationAnalyzer ~120 ms, FieldNamingAnalyzer ~125-160 ms, CommentTextAnalyzer ~57 ms, NullCheckAnalyzer ~35 ms |
| StyleBro after the walk merges below | 709-752 ms (three runs, alternated with the row above) | FieldNamingAnalyzer ~150-200 ms, DocumentationAnalyzer ~45-70 ms, CommentTextAnalyzer ~33 ms, NullCheckAnalyzer ~6 ms |

Single runs vary by about 20% (the two 2026-10-03 runs of the same build differ by 25%). In real builds the analyzers run concurrently with each other and with the compiler,
so the wall-clock cost is smaller: the private 30-project app built in the same time with and without StyleBro.

2026-10-03: DirectiveSpacingAnalyzer walked every token (trivia included) of every file; it now walks only the
directives and skips files without any (45 -> 23 ms). The blank-line analyzers each walk all tokens; merging them into
one pass is the next step if analyzer time ever matters.

2026-10-04: DocumentationAnalyzer walked each tree's trivia four times and CommentTextAnalyzer twice (once into the XML
of every doc comment); each now walks once and keeps only the comments. NullCheckAnalyzer built the operation tree for
every null check to rule out expression trees; it now does so only inside a lambda or query. WrappingPlacementAnalyzer
skips operators with no line break around them before building trivia lists. Same diagnostics before and after.
`STYLEBRO_BENCH_ONLY=Name1,Name2` times only those analyzers (alone they pay shared costs such as building the red tree
or binding, so compare builds, not analyzers).
