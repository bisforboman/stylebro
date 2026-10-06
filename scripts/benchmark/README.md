# Analyzer benchmark

Times analyzers the way the compiler's `ReportAnalyzer` does (each analyzer's own execution time), but without the
noise of a full build: one compilation of a folder of sources, all analyzers together, single-threaded, the median of
several runs after a warm-up run. Since 2026-10-05 every run (and every build in compare mode) gets freshly parsed
sources, as a real build does: nothing one run built (red nodes, structured trivia, a per-tree cache) carries over to
the next, so the analyzer that walks a tree first pays for it. Numbers from before then aren't comparable.

```bash
dotnet build src/StyleBro.Analyzers
dotnet run -c Release --project scripts/benchmark -- src/StyleBro.Analyzers/bin/Debug/netstandard2.0/StyleBro.Analyzers.dll path/to/sources 5
```

Compare two builds (what CI's `performance` job does on every pull request, base branch against the PR):

```bash
dotnet run -c Release --project scripts/benchmark -- compare base/StyleBro.Analyzers.dll head/StyleBro.Analyzers.dll path/to/sources 10
```

Both builds load into one process and take turns on the same compilation (which goes first alternates, with a garbage
collection before each), and each analyzer's fastest run counts: other work on the machine only adds time. It exits
with 1 when the head build is clearly slower: the total by more than 10 % and 25 ms, one analyzer by more than 50 % and
15 ms, or a new analyzer above 60 ms. A slowdown is measured for up to two more rounds before it counts, so noise goes
away and a real regression stays. Checked on a busy machine: a build against itself passed three times out of three,
and the build before the 2026-10-04 speed-ups against the one after failed on DocumentationAnalyzer, CommentTextAnalyzer
and the total.

The optional fourth argument sets preprocessor symbols (`HAVE_ASYNC;NET8_0`). Missing references don't matter much:
the sources are compiled against the .NET runtime only, so code using other packages binds partly. Compare two builds
of the analyzers on the same sources rather than reading the absolute numbers.

## Results (2026-10-02, Newtonsoft.Json's main project, 242 files)

| | All analyzers | Slowest |
|---|---|---|
| StyleBro 0.1.0-alpha.8 | 1,096 ms | FieldNamingAnalyzer 468 ms |
| StyleBro after caching type facts in FieldNamingAnalyzer | ~800 ms | DocumentationAnalyzer ~160 ms, FieldNamingAnalyzer ~150 ms |
| StyleCop.Analyzers 1.2.0-beta.556 (182 analyzers), old method (not comparable, see the 2026-10-05 row) | 1,671 ms | SA1121 152 ms, SA1101 108 ms |
| StyleBro, 2026-10-03 (88 rules, 46 analyzers) | 1,082-1,350 ms (two runs) | DocumentationAnalyzer ~180-210 ms, FieldNamingAnalyzer ~140-200 ms, CommentTextAnalyzer ~115-200 ms, BlankLineRunsAnalyzer ~110-120 ms |
| StyleBro, 2026-10-04 (55 analyzers) | 806-858 ms (three runs) | DocumentationAnalyzer ~120 ms, FieldNamingAnalyzer ~125-160 ms, CommentTextAnalyzer ~57 ms, NullCheckAnalyzer ~35 ms |
| StyleBro after the walk merges below | 709-752 ms (three runs, alternated with the row above) | FieldNamingAnalyzer ~150-200 ms, DocumentationAnalyzer ~45-70 ms, CommentTextAnalyzer ~33 ms, NullCheckAnalyzer ~6 ms |
| StyleBro after the cheap-checks-first changes below | 669-743 ms (three runs, alternated with 670-747 ms for the row above) | CamelCaseNamingAnalyzer 17 -> 2 ms, BaseCallsAnalyzer 15 -> 5 ms, CallChainAnalyzer ~20 -> 14 ms, EmbeddedCommentAnalyzer ~29 -> 24 ms |
| StyleBro, 2026-10-05, fresh sources per run (compare mode, fastest run each) | 595 ms | FieldNamingAnalyzer 59 ms, DocumentationAnalyzer 51 ms, BlankLineAfterAnalyzer 47 ms, BlankLineRunsAnalyzer 45 ms |
| StyleBro with one shared walk per tree (`TreeWalk`, same run) | 386 ms | FieldNamingAnalyzer 53 ms, DocumentationAnalyzer 37 ms, CommentSpacingAnalyzer 32 ms |
| StyleCop.Analyzers 1.2.0-beta.556 (182 analyzers) vs StyleBro `main` (66 analyzers), 2026-10-05, compare mode, three runs on a busy machine | StyleCop 1,291-2,171 ms, StyleBro 411-687 ms (3.1-3.4x in each run) | SA1121 88-159 ms, SA1101 85-143 ms; FieldNamingAnalyzer 52-72 ms, DocumentationAnalyzer 36-66 ms |

Single runs vary by about 20% (the two 2026-10-03 runs of the same build differ by 25%). In real builds the analyzers run concurrently with each other and with the compiler,
so the wall-clock cost is smaller: the private 30-project app built in the same time with and without StyleBro.

2026-10-03: DirectiveSpacingAnalyzer walked every token (trivia included) of every file; it now walks only the
directives and skips files without any (45 -> 23 ms). The blank-line analyzers each walk all tokens; merging them into
one pass is the next step if analyzer time ever matters.

2026-10-04: DocumentationAnalyzer walked each tree's trivia four times and CommentTextAnalyzer twice (once into the XML
of every doc comment); each now walks once and keeps only the comments. NullCheckAnalyzer built the operation tree for
every null check to rule out expression trees; it now does so only inside a lambda or query. WrappingPlacementAnalyzer
skips operators with no line break around them before building trivia lists. Same diagnostics before and after.
Then cheap checks first: CamelCaseNamingAnalyzer checks the name before looking up the symbol (most names need no
rename), BaseCallsAnalyzer rules out virtual calls before the speculative bind, CallChainAnalyzer skips chains on one
line, EmbeddedCommentAnalyzer skips braces without a comment before them. Same diagnostics again.
FieldNamingAnalyzer, profiled with `dotnet-trace` (most of its time was in `TypeFacts.For`): it walked each type's
tokens INTO trivia to find strings in directives, which made Roslyn build the XML structure of every documentation
comment; it now opens only directives, and skips the trivia walk for declarations without any (-25 to -30 % in
alternated runs, same diagnostics).
2026-10-05: one walk per tree. A bare walk of every token of Newtonsoft.Json costs ~35-40 ms (a probe analyzer that
does nothing else), and about 16 analyzers each walked every token, trivia or node of every file. `TreeWalk` (in
StyleBro.Analyzers) walks a tree once and keeps its tokens and nodes as arrays (trivia comes from the tokens) while the
tree lives; reading the array costs ~2 ms. The analyzers stay separate (ids, tests and fixes unchanged); whichever
runs first pays the walk. 595 -> 386 ms, same diagnostics.
`STYLEBRO_BENCH_ONLY=Name1,Name2` times only those analyzers (alone they pay shared costs such as building the red tree
or binding, so compare builds, not analyzers).
