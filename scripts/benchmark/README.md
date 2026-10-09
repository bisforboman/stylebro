# Analyzer benchmark

Times analyzers the way the compiler's `ReportAnalyzer` does (each analyzer's own execution time), but without the
noise of a full build: one compilation of a folder of sources, all analyzers together, single-threaded, the median of
several runs after a warm-up run. Since 2026-10-05 every run (and every build in compare mode) gets freshly parsed
sources, as a real build does: nothing one run built (red nodes, structured trivia, a per-tree cache) carries over to
the next. Since 2026-10-09 the shared walk of every tree (`TreeWalk`: tokens, nodes, trivia) is built before the
analyzers run and timed as its own line, `TreeWalk (shared walk of every tree)`, which counts in the total and is
compared like an analyzer; before, the analyzer that touched a tree first paid for it (see FieldNamingAnalyzer below).
Numbers from before then aren't comparable.

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

`--all-rules` (any mode) also turns on the rules that are off by default (through the compilation options, which
`Severities.IsOn` reads too). Checked 2026-10-09 on Newtonsoft.Json: 4,087 reports without it, 4,439 with it (BRO1143 39,
BRO1145 9, BRO1147 18, BRO1148 17, BRO1310 1, BRO1313 6, BRO1409 263; BRO1139 23 -> 21, it leaves an `else` after a jump
to BRO1143; BRO1312 and BRO1314 report on a probe file, nothing in Newtonsoft.Json). CI's `performance` job compares with
`--all-rules`: a superset, so the off-by-default analyzers are measured too.

The optional fourth argument sets preprocessor symbols (`HAVE_ASYNC;NET8_0`). Missing references don't matter much:
the sources are compiled against the .NET runtime only, so code using other packages binds partly. Compare two builds
of the analyzers on the same sources rather than reading the absolute numbers.

## Per file (IDE)

Visual Studio analyzes the open document on every edit, in a new compilation: the syntax tree, syntax node and semantic
model actions of that file, and the symbol actions of the symbols declared in it. Whole-project numbers hide an analyzer
that is slow on one big file or grows faster than the file, so `file` times one document at a time (the target tree's `TreeWalk` line included):

```bash
dotnet run -c Release --project scripts/benchmark -- file path/to/StyleBro.Analyzers.dll path/to/sources 10 7 "SYMBOL_A;SYMBOL_B" --all-rules
dotnet run -c Release --project scripts/benchmark -- file path/to/StyleBro.Analyzers.dll path/to/sources "a.cs;b.cs" 7
```

The third argument is a number (the N largest files of the folder, default 10) or `;`-separated files, which may lie
outside the folder (they are compiled with it). For every run each file gets a freshly parsed tree in a new compilation
(the other files are parsed once), its semantic model is bound first (not timed: the IDE has usually bound it), and a new
`CompilationWithAnalyzers` (its telemetry adds up per instance) runs `GetAnalyzerSyntaxDiagnosticsAsync(tree)` and
`GetAnalyzerSemanticDiagnosticsAsync(model, null)`. The runs go round the files, each analyzer's fastest run counts. It
prints each file's slowest analyzers, the slowest analyzer per file with microseconds per KB, and the reports per
analyzer (to check that a change keeps the diagnostics). `STYLEBRO_BENCH_CSV=path` also writes every
file/analyzer/time/reports row. Shared costs land on whichever analyzer comes first: building the tree's red nodes, and the
driver's generated-code check (a walk of the whole tree on a file's first reported diagnostic).
So an analyzer that reports little and is first can look slow; profile before blaming it (`dotnet-trace collect --format
speedscope -- dotnet scripts/benchmark/bin/Release/net10.0/Benchmark.dll file ...` with `STYLEBRO_BENCH_ONLY`).

Results (2026-10-05, `--all-rules`, 7 runs, three alternated rounds of origin/main and this change, fastest run each; the
machine was busy with other builds and medians were 2-3x the fastest runs, so read the differences, not the absolute
numbers). Newtonsoft.Json's 10 largest files with its netstandard2.0 symbols (LinqBridge.cs is `#if !HAVE_LINQ`, so
empty), and the largest files of Jellyfin's MediaBrowser.Controller (compiled without its project references):

| File | KB | Before | After | Slowest analyzer after |
|---|---:|---:|---:|---|
| NJ Serialization/JsonSerializerInternalReader.cs | 128 | 30.0 ms | 24.8 ms | CommentSpacingAnalyzer 2.3 ms |
| NJ Linq/JToken.cs | 112 | 34.5 ms | 24.7 ms | FieldNamingAnalyzer 2.9 ms |
| NJ JsonTextReader.cs | 102 | 26.7 ms | 21.9 ms | FieldNamingAnalyzer 3.0 ms |
| NJ JsonWriter.Async.cs | 94 | 16.2 ms | 13.6 ms | DocumentationAnalyzer 1.9 ms |
| NJ Converters/XmlNodeConverter.cs | 84 | 58.2 ms | 23.8 ms | InternalTypeMethodAnalyzer 2.8 ms |
| NJ JsonTextReader.Async.cs | 79 | 19.0 ms | 15.4 ms | FieldNamingAnalyzer 2.6 ms |
| NJ JsonTextWriter.Async.cs | 79 | 22.8 ms | 19.4 ms | BaseCallsAnalyzer 3.3 ms |
| NJ Serialization/DefaultContractResolver.cs | 77 | 18.0 ms | 16.6 ms | CommentSpacingAnalyzer 1.4 ms |
| NJ JsonWriter.cs | 65 | 18.9 ms | 14.6 ms | DocumentationAnalyzer 1.5 ms |
| Jellyfin MediaEncoding/EncodingHelper.cs | 348 | 170.1 ms | 98.9 ms | EmbeddedCommentAnalyzer 10.6 ms (shared costs, see above) |
| Jellyfin Entities/BaseItem.cs | 103 | 43.5 ms | 37.4 ms | PascalCaseNamingAnalyzer 4.6 ms |
| Jellyfin Entities/Folder.cs | 78 | 90.6 ms | 27.0 ms | DocumentationAnalyzer 6.0 ms |

Scaling (a file's namespace block repeated 1, 2, 4 and 8 times, namespaces renamed): before, the total grew 20x for 8x
the size of JsonTextReader.cs (31 -> 616 ms) and ElseIfAnalyzer alone 405 ms at 787 KB; after, 23 -> 192 ms (8.4x), and
no analyzer grows clearly faster than the file.

What was slow, all with the same diagnostics before and after (reports per analyzer compared on every file):

- ElseIfAnalyzer (BRO1139, quadratic): for each `else` whose join might change the brace rules' findings it parsed the
  whole joined FILE again (~10 ms per candidate in a 100 KB file, 47 ms on EncodingHelper.cs). Now an incremental reparse
  (`SyntaxTree.WithChangedText`), as in `Braces.AddToExpansion` (the fix side).
- InternalTypeMethodAnalyzer (BRO1409, off by default): asked every interface member of every type of the compilation
  for its implementation, once per compilation, and the IDE makes a new compilation per edit (58 ms on Folder.cs, 30 ms
  on XmlNodeConverter.cs). Now only the method's type and the types derived from it, and only interface members with
  the method's name (derived types from one cheap walk over the type declarations per compilation).
- CamelCaseNamingAnalyzer (BRO1301/BRO1302): walked the whole member (into doc comments) once per variable or parameter
  it reports; the scope's identifiers are now collected once. The Hungarian prefix settings (BRO1310) were parsed for
  every variable; now once per options object.
- FieldNamingAnalyzer: walked every trivia of a type with any directive to find disabled code; now goes from directive to
  directive, and reads the type's tokens from `TreeWalk`'s shared array.

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
| StyleBro, 2026-10-05, before / after the per-file fixes (compare mode, busy machine) | 600 / 571 ms | ElseIfAnalyzer 27 -> 10 ms, FieldNamingAnalyzer 82 -> 78 ms |
| StyleCop.Analyzers 1.2.0-beta.556 (182 analyzers) vs StyleBro `main` (66 analyzers), 2026-10-05, compare mode, three runs on a busy machine | StyleCop 1,291-2,171 ms, StyleBro 411-687 ms (3.1-3.4x in each run) | SA1121 88-159 ms, SA1101 85-143 ms; FieldNamingAnalyzer 52-72 ms, DocumentationAnalyzer 36-66 ms |
| StyleBro, 2026-10-07, before / after round 3 below (72 analyzers, compare mode, 10 runs, two runs on a busy machine) | 354-371 / 226-254 ms (-31 to -36 %) | EmbeddedCommentAnalyzer 23 -> 0.3 ms, DirectiveSpacingAnalyzer 20 -> 0.6 ms, CommentTextAnalyzer 19-25 -> 8 ms, ParameterLayoutAnalyzer 15 -> 4 ms; FieldNamingAnalyzer 43-74 ms either way (it pays the shared walk on many files) |

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
`STYLEBRO_BENCH_ONLY=Name1,Name2` times only those analyzers (alone they still pay Roslyn's shared costs, such as the
driver's generated-code check on the first diagnostic in a file, so compare builds, not analyzers).
2026-10-07 (round 3), same diagnostics on Newtonsoft.Json, Serilog, FFMpegCore and StyleBro's own `src` (reports per
analyzer compared, with and without `--all-rules`):
- `TreeWalk.Trivia` is a cached array of the trivia that aren't whitespace or line breaks (comments, documentation,
  directives, disabled code). Seven analyzers walked every trivia of every token, and only want those; the array is
  built once per tree. `TreeWalk.Tokens` and `TreeWalk.Nodes` come from one `DescendantNodesAndTokens()` walk.
- DirectiveSpacingAnalyzer and FieldNamingAnalyzer's type facts find directives in that array; `GetNextDirective`
  searched the tree again from each directive.
- EmbeddedCommentAnalyzer starts from the comments (few) instead of asking every node's `{` for the token before it.
- Cheap checks first: ParameterLayout's and ParenthesisPlacement's layout checks before the trivia of every item or
  the token before the list, ListGaps skips lists on one line, BlankLineRuns skips the `}` without a line break in its
  leading trivia before finding the previous token and judges the gap after a `}` by its trivia before its lines.
- FieldNamingAnalyzer looks for a field's old name only in the strings that contain it, instead of splitting every
  string of the type into words.

Tried and dropped (no gain in alternated runs): skipping MemberOrdering's initializer/serialization guards for types
already in order and a `ContainsDirectives` shortcut in DocumentationAnalyzer's directive check; not kept either:
explicit equality for `FieldStyles` and a per-tree cache of the generated-file check (both run only for rename
candidates; the profile blamed the struct's reflection-based hash). `dotnet-trace`'s sampling profile lands on safepoints (`PollGC`, `Monitor.Enter`),
so it points at the right analyzer but overstates single methods: confirm each idea with `compare`.

2026-10-09, FieldNamingAnalyzer's variance: in compare runs of one build against itself it took 72-101 ms (fastest of 10
runs), alone 120-250 ms, while most analyzers were stable. It is StyleBro's only symbol-action analyzer, and the driver
runs symbol actions before a file's tree and node actions, so on most files it was the first to ask for `TreeWalk`'s
arrays: it paid for the whole walk (~65 ms, allocation-heavy, so also for the garbage collections that came with it) and
for the driver's generated-code check on its first diagnostic in a file. Built and timed before the analyzers (above),
the walk measures 66-91 ms on a busy machine and FieldNamingAnalyzer 17-29 ms with all analyzers. Alone it still took
53-109 ms: `NamespaceNames.IsGenerated` built the text of the file's whole header (Newtonsoft.Json's license comment) for
every rename candidate; it is cached per tree now: alone 30-35 ms in five of six runs (one at 86 ms, with the base build
at 79 ms in the same run), against 53-109 ms before (same reports on Newtonsoft.Json, Serilog and StyleBro's `src`, with
and without `--all-rules`). Alone it still pays the driver's generated-code check. A profile with `dotnet-trace` showed the rest of its variance
as `Monitor.Enter` waits: thread suspension for garbage collections, which land on whatever runs.
