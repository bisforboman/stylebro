# dotnet format's fix loop, per id (October 2026)

Status: **measurement report, for the owner's review** (owner's choice 2026-10-22: measure which ids cost the most in
`dotnet format`'s fix loop before phase 3 of docs/proposals/format-speed.md). Nothing in `src/` was changed. A follow-up
to [performance-audit-2026-10.md](performance-audit-2026-10.md), section 3, which found that the loop dominates but
couldn't say which id each step was.

## Summary

- **The analysis before each id is the cost, not the fixes.** On three repositories, Fix All is 10-23 % of the fix
  phase. The other 77-90 % is dotnet format analyzing again before each id: Newtonsoft.Json 565-922 s against 82-98 s of
  Fix All, Shouldly 47-49 s against 11-14 s, Serilog 61-66 s against 15-17 s. The analysis before an id costs roughly the
  same whatever the id: a median of 7.6-8.9 s per id on Newtonsoft.Json, 0.7-0.8 s on Shouldly, 1.0-1.2 s on Serilog. It
  grows with the number of project copies the previous fix touched and that have to compile again, not with the
  analyzer. Newtonsoft.Json's 8 framework copies of the library and 3 of the tests account for all of it; one syntax-only
  analyzer (BRO1406, 40 findings) took 0.6 s on one copy and 104 s on another in the same run, a stall the second run
  didn't have.
- **No new BRO1112-style regression.** The ids whose Fix All is large next to their edits are the renames (BRO1303,
  BRO1305, BRO1306, BRO1308, BRO1311). Their solution-wide guards are designed to do that (Roslyn's FindReferences and
  FindDerivedClasses build their indexes over every document; BRO1311 compiles every project to check that a rename adds
  no error). BRO1112's Fix All after PR #277 is 5.4-6.0 s for 905 files (387 s before).
- **One avoidable cost in the rename fixes, measured.** dotnet format calls the fixer's `RegisterCodeFixesAsync` on one
  diagnostic only to read the equivalence key, and then runs Fix All. Both rename providers run their whole kept-rename
  check there (`GetKeptReasonAsync`), so each id pays for one guard check before Fix All repeats it. BRO1311 on Shouldly,
  traced: 8.3 s of thread time in `RegisterCodeFixesAsync` (most of it `TupleElementRenamer.AddsErrorsAsync`, compiling
  every project), 4.1 s in Fix All. Proposed fix below.
- **One suspected bug, not root-caused: order-dependent rename output.** Seven Serilog runs changed the same 173 files.
  Four wrote `Interlocked.Exchange(ref Serilog.Log.logger, ...)` in `src/Serilog/Log.cs` and three wrote `ref
  Log.logger`, depending on the order dotnet format picked for the ids. Both compile. Details below.
- **Merged fix rounds (phase 3) would take an estimated 34-68 % off Newtonsoft.Json's fix phase (220-570 s of 650-1,020
  s), 17-58 % off Shouldly's (10-35 s of 60 s) and 8-59 % off Serilog's (7-46 s of 77-79 s).** Those are the estimate's
  low and high bounds; the repositories need 7-8 rounds instead of 45-71 per-id steps (method below).

## Method

**The probe.** dotnet format (SDK 10.0.113, `DotnetTools/dotnet-format`) fixes one id at a time in
`AnalyzerFormatter.FixDiagnosticsAsync`. For each id with findings it runs the analyzers supporting the id over every
project with findings (`AnalyzerRunner.RunCodeAnalysisAsync`), then `SolutionCodeFixApplier.ApplyCodeFixesAsync`, which
calls `RegisterCodeFixesAsync` on the first diagnostic and then the Fix All. It never logs which id it is fixing, and the
order of the ids changes between processes (a hash-set order). `scripts/benchmark/FormatProbe` is a startup hook
(`DOTNET_STARTUP_HOOKS`) that patches those three methods with Lib.Harmony 2.4.3 (2.4.1 refuses runtime 10.0.13) and
writes one line per call: the project and milliseconds per analysis, and per Fix All the id, its milliseconds, the
diagnostics it was given and the documents and files it changed.

It costs no measurable time: the same repositories took 733-1,121 s (Newtonsoft.Json) and 82-83 s (Shouldly) with the
probe, against 746-1,128 s and 82.2 s without it in the audit. Harmony's JIT hook shows up in traces, wrapping the real
JIT time.

**The runs.** `scripts/benchmark/Measure-FixLoop.ps1` clones a repository at its commit and adds StyleBro's Release DLLs
(main at `73015a7`, after PR #277) through a `CustomAfterMicrosoftCommonTargets` hook, as the real-world check does: every
BRO id, each rule at its own default severity, plus the newer presets' rules, no preset options. It then runs
`dotnet format analyzers <sln> --diagnostics <every BRO id> --severity warn --no-restore -v diag` on a clean checkout,
several times, and keeps each run's log, probe lines and diff (with its md5).
`scripts/benchmark/summarize-fixloop.py` makes the tables; `scripts/benchmark/fixloop-rounds.py` makes the estimate.

**The repositories.**
- **Newtonsoft.Json** at the real-world pin `52fa3ae`, 2 runs. 17 project copies: 8 frameworks of the library, 3 of the
  tests, 2 of the test console, fuzz tests. Its `Src/global.json` asks for SDK 10.0.300, so the script removes every
  global.json.
- **Shouldly** `b9b5293`, 3 runs: snapshot tests (633 `*.approved.*` files), multi-targeted (trial 37).
- **Serilog** at the real-world pin `bebc771`, 3 + 4 runs: multi-targeted, 5 frameworks; restored with
  `-p:NuGetAudit=false`.
- **FFMpegCore**, 1 run, as a smoke test.

**Profiles.** For the outliers, each id ran alone (`--diagnostics BROxxxx`), with `dotnet-trace collect -p` attached
once the probe reported the fix phase. Launching dotnet-format under `dotnet-trace collect --` doesn't work: the
workspace's BuildHost child process inherits the diagnostic-port setting and waits for a tracer forever (a
`TimeoutException` while loading the workspace). The traces were read with `scripts/benchmark/speedscope-top.py`.

**Machine and noise.** One cloud container: 4 vCPUs, 15 GB, Linux, .NET SDK 10.0.113. Nothing else ran during the
measured runs (an accidental parallel run was discarded and rerun). dotnet format's memory peaked at 4.8 GB.
- Shouldly and Serilog were steady: totals within 2 % (82-83 s and 109-114 s), every id within about a second.
- Newtonsoft.Json's two runs gave the same diff but took 1,121 s and 733 s: run 1 lost ~165 s in one id's analysis
  (BRO1406: 104 s on one framework copy against 0.6 s on another) and was 10-60 % slower on most other ids. So single
  ids' times on Newtonsoft.Json are given as the range of the two runs; read differences under ~2x there as noise.

## Where the fix phase goes

| | Newtonsoft.Json (2 runs) | Shouldly (3) | Serilog (7) |
|---|---:|---:|---:|
| Total `dotnet format` | 733-1,121 s | 82-83 s | 109-114 s |
| First analysis (all analyzers, every project) | 59-70 s | 10 s | 18-19 s |
| Ids fixed | 71 | 45 | 47-48 |
| Fix phase: analysis before each id | 565-922 s | 47-49 s | 61-66 s |
| Fix phase: Fix All (incl. `RegisterCodeFixesAsync`) | 82-98 s | 11-14 s | 15-17 s |
| Analysis before an id, median / largest | 7.6-8.9 s / 19-169 s | 0.7-0.8 s / 4 s | 1.0-1.2 s / 3-6 s |
| Files changed | 938 | 245 | 173 |
| Same diff every run | yes | yes | **no** (2 variants, below) |

- **Where the analysis goes:** the projects the previous id's fix changed, compiled again. Newtonsoft.Json: tests 55 %,
  library 45 % (8 framework copies of the same files), TestConsole and FuzzTests ~0. Shouldly: the library 71 %, tests
  26 %. Serilog: the library 63 %, tests 34 %.
- **Analyzer kind matters less than recompiling.** After BRO1136 changed 2 library files, BRO1103 (semantic, 72
  findings) took 1.2-2.7 s per framework copy. The cheapest steps were 0.4-0.9 s, for ids whose last fix touched few
  projects.
- **Ids that found nothing:** none in these runs. dotnet format re-analyzes only ids that had findings in the first pass,
  and every one still had them.
- **Each id's Fix All is called once per project.** dotnet format collects the fixers of every project's analyzer
  references and doesn't deduplicate them: 17 calls per id on Newtonsoft.Json, 12 on Shouldly, 15 on Serilog. Only the
  first changes anything; the others returned in under 1 ms here (their diagnostics point at documents the first call
  replaced). It's harmless now, but it would cost time whenever the first diagnostic sits in a document the fix didn't
  change. The in-process host (format-speed.md (a)) should call each fixer once.

## The most expensive ids

Newtonsoft.Json, both runs (range), sorted by the median of analysis + Fix All. "Diagnostics" is everything the id's
analyzers reported in that step, other ids of the same analyzer included (the Fix All picks its own id from them).

| Id | Analysis before it (s) | Fix All (s) | Diagnostics | Files changed | Fix All per file (ms) |
|---|---:|---:|---:|---:|---:|
| BRO1406 | 3.4-168.6 (the stall) | 0.1-1.4 | 40 | 2 | 386 |
| BRO1303 | 14.8-18.7 | 22.8-25.6 | 2,397 | 137 | 177 |
| BRO1604 | 15.6-33.0 | 2.7-5.4 | 1,601 | 18 | 224 |
| BRO1138 | 16.2-38.4 | 0.7-1.2 | 2,005 | 77 | 12 |
| BRO1107 | 8.9-42.3 | 0.3-0.6 | 303 | 27 | 17 |
| BRO1133 | 17.0-28.1 | 2.0-2.2 | 10,752 | 173 | 12 |
| BRO1404 | 16.8-27.4 | 0.1-0.2 | 134 | 10 | 18 |
| BRO1308 | 7.0-12.3 | 6.8-17.9 | 1,979 | 2 | 6,198 |
| BRO1306 | 10.9-20.8 | 3.7-8.4 | 2,474 | 10 | 602 |
| BRO1106 | 16.6-22.2 | 0.3-0.5 | 635 | 36 | 11 |
| BRO1105 | 15.0-23.2 | 0.1-0.2 | 106 | 15 | 11 |
| BRO1603 | 16.9-20.2 | 0.1 | 1,733 | 8 | 15 |
| BRO1001 | 15.8-19.2 | 0.6-1.8 | 931 | 104 | 12 |
| BRO1104 | 15.3-20.7 | 0.2-0.3 | 136 | 10 | 27 |
| BRO1519 | 11.3-23.1 | 0.8-1.1 | 4,102 | 93 | 10 |
| BRO1611 | 7.5-17.3 | 2.6-4.7 | 1,510 | 4 | 919 |
| BRO1112 | 8.7-11.7 | 5.4-6.0 | 7,556 | 905 | 6 |
| BRO1111 | 14.1-16.0 | 0.2-0.3 | 246 | 15 | 14 |
| BRO1405 | 10.4-16.1 | 0.7-1.1 | 1,808 | 74 | 12 |
| BRO1305 | 7.9-15.7 | 1.4-3.1 | 23 | 2 | 1,127 |

Shouldly (3 runs), the top 5: BRO1603 (analysis 3.7-4.0 s, Fix All 1.0-1.2 s, 61 files), BRO1311 (0.9-1.7 s, 2.4-3.9 s,
**1 file**), BRO1303 (1.4-2.7 s, 0.7-2.2 s, 58 files), BRO1521 (0.8-3.9 s, 0.3-0.4 s, 4 files), BRO1514 (2.3-4.1 s,
0.3-0.5 s, 52 files).

Serilog (7 runs), the top 5: BRO1303 (2.7-5.1 s, 5.6-8.6 s, 79 files), BRO1001 (2.6-4.6 s, 0.3-0.7 s, 39 files), BRO1404
(2.4-4.9 s, 0.2-1.1 s, 144 files), BRO1106 (2.3-6.4 s, 0.1-0.2 s, 13 files), BRO1603 (2.0-3.5 s, 0.1-0.2 s, 14 files).

Across the three repositories:
- **BRO1303** (private field casing) is the most expensive Fix All everywhere: 23-26 s on Newtonsoft.Json, 6-9 s on
  Serilog. It renames the most (137 files on Newtonsoft.Json), and every candidate goes through FindReferences and the
  guards, solution-wide.
- **The other renames** cost seconds for a few files: BRO1308 7-18 s for 2 files (1,979 candidate diagnostics, almost all
  kept by guards), BRO1306 4-8 s for 10, BRO1305 1.4-3.1 s for 2, BRO1311 2.4-3.9 s for 1.
- **The documentation fixes with semantic checks** (BRO1604, BRO1611, BRO1613) cost 2.6-5.4 s on Newtonsoft.Json for
  4-18 files. Alone, BRO1613 took 0.56 s for its one file (against 3.2 s in the full loop) and its trace has no StyleBro
  hot spot (0.3 s of thread time in StyleBro.CodeFixes). The loop's extra time is Roslyn bringing the semantic model up
  to date after the earlier fixes, not the fixer.
- **Everything else:** under ~2 s of Fix All and ~5-30 ms per changed file.

### Outliers, traced

- **BRO1308 alone** (Newtonsoft.Json, 56 diagnostics = 7 fields x 8 framework copies, 2 files changed): Fix All 10.0 s.
  Thread time:
  - Roslyn's `FieldSymbolReferenceFinder.DetermineDocumentsToSearchAsync` 20 s (the syntax index of every document of
    the 17 projects);
  - `CamelCaseRenamer.GetItemChangesAsync` 5 s, `IsHiddenByNewMemberAsync` 3.5 s (`SymbolFinder.FindDerivedClassesAsync`);
  - `CamelCaseNamingCodeFixProvider.RegisterCodeFixesAsync` 1.9 s, the check done before Fix All.

  It is the guards' designed cost, paid per candidate: the indexes are built once per solution snapshot, so the first
  rename id after a fix that changed many documents pays for the whole solution.
- **BRO1311 alone** (Shouldly, 6 diagnostics, 1 file): 12.9 s under the trace (2.4-3.9 s in the loop without it).
  - Thread time: `TupleElementNamingCodeFixProvider.RegisterCodeFixesAsync` 8.3 s, Fix All 4.1 s, of which
    `TupleElementRenamer.AddsErrorsAsync` / `Errors(Compilation)` 9.4 s across both.
  - The safety net ("a rename that adds compile errors is dropped") compiles every project. It runs once for the single
    diagnostic dotnet format registers and again in Fix All.
- **BRO1613 alone**: see above, no fix-side cause.

**Proposed fix (small, the owner decides).** Today `CamelCaseNamingCodeFixProvider.RegisterCodeFixesAsync` and
`TupleElementNamingCodeFixProvider.RegisterCodeFixesAsync` await `GetKeptReasonAsync` before registering the action:
- Option 1: register the action at once and decide inside its callback (a kept rename returns the solution unchanged).
  `RegisterCodeFixesAsync` then costs nothing.
- Option 2: keep the check where it is, but cache its result per `Solution` + diagnostic, so Fix All reuses what the
  registration computed.

What it saves: the registration's share of each rename id's Fix All, about two thirds for BRO1311 on Shouldly, less for
BRO1303 (one candidate of hundreds). That is a few seconds per rename id per dotnet format process; `bro format` starts
several processes per run.

What to check: the IDE lightbulb would offer, then do nothing for, a kept rename with option 1 (the comment in both
providers says it shouldn't). Option 2 keeps that behaviour. A test can count `GetKeptReasonAsync` calls per Fix All, as
`FixAllThatSorts_ScansTheSolutionOnce` does for the names scan.

### Suspected bug: BRO1303's qualifier depends on the fix order

Serilog, 7 runs, all with 173 changed files.
- 4 runs (md5 `D1-B6-86-...`) wrote `var logger = Interlocked.Exchange(ref Serilog.Log.logger, Serilog.Core.Logger.None);`
  in both `Log.CloseAndFlush` and `Log.CloseAndFlushAsync`.
- 3 runs (`8C-FE-...`, `BB-5C-...`) wrote `ref Log.logger`. Everything else in the diffs was identical.

The rename is BRO1303's (`_logger` -> `logger`). A local named `logger` hides the field there, so the fix qualifies it
with `field.ContainingType.ToMinimalDisplayString(model, span.Start)` (`src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs`,
`GetReplacementAsync`, line 1046).

Serilog has no other type named `Log`, so `Serilog.Log` shouldn't be the minimal name inside the class. That suggests
the semantic model or position used comes from a document state where the reference sits outside the namespace. One
guess is a copy whose text another fix has already shifted.

No single id explains it: neither "X ran before BRO1303" nor its opposite separates the two groups of runs. Six other
ids edit Log.cs in the same run: BRO1404 (`private`), BRO1603, BRO1604, BRO1619 (moves the `<remarks>` above the class,
which shifts every later position), BRO1107 and BRO1108.

Both outputs compile and are stable on the next run, so it is a determinism problem, not a broken build. Next step: a
FixOrderTests case built from Log.cs with BRO1303 and the ids that edit the file, which should reproduce it in a unit
test.

## What merged fix rounds would save

`scripts/benchmark/fixloop-rounds.py` models phase 3 (format-speed.md, option (b)):
- **Interaction:** two ids interact when they appear together in a FixOrderTests case (135 cases with 2+ ids, 255 pairs,
  81 ids); every other pair is assumed independent.
- **Rounds:** the ids a repository fixes go into rounds with no interacting pair (greedy colouring, most-connected first).
  The renames (BRO13xx) get their own last round, since they interact with anything.
- **Cost of a round:** one analysis pass, then its ids' Fix Alls. The Fix All time stays what it is today.
- **Bounds on a round's analysis:**
  - *high*: its ids' analyses added up, capped at the run's first analysis (every analyzer, every project);
  - *low*: its most expensive id's analysis, because the work is recompiling the changed projects, which a round does
    once.

| | Newtonsoft.Json | Shouldly | Serilog |
|---|---:|---:|---:|
| Ids fixed (per-id steps today) | 71 | 45 | 47-48 |
| Ids that interact with another fixed id | 51 | 33 | 36-38 |
| Rounds | 8 | 8 | 6-7 |
| Fix phase today | 647-1,020 s | 59-60 s | 77-79 s |
| Saving, low-high | 219-568 s (34-68 %) | 10-35 s (17-58 %) | 7-46 s (8-59 %) |
| Saving, medians of the runs' bounds | 342-504 s | 11-31 s | 8-45 s |

The rounds come out as chains: one large round, then shrinking ones. For example, Newtonsoft.Json's rounds:
1. 28 ids
2. 16 ids
3. 6 ids
4. 6 ids
5. 6 ids
6. 2 ids
7. 2 ids, BRO1001 + BRO1521
8. the 5 renames

Most rounds after the first exist because FixOrderTests cases chain ids together (BRO1001 meets BRO1409, BRO1112,
BRO1505, ...), not because those ids touch the same code in this repository. A finer interaction list, made by
format-speed.md's "merged vs. sequential on the rule test sources" check, would merge more.

Against the whole `dotnet format` run, that is 30-60 % of Newtonsoft.Json (733-1,121 s), 12-43 % of Shouldly (82 s) and
6-41 % of Serilog (110 s).

## Caveats

- **The interaction model is optimistic.** Two ids that FixOrderTests never puts together may still edit neighbouring
  text in a real file. A merged round can then give a different result, or need one more round, and the convergence
  check that `bro format` runs anyway would show it. The model also ignores the extra full analysis to confirm a merged
  run is clean: about one first analysis (10-70 s), which is less than one round on Newtonsoft.Json and about one round
  elsewhere.
- **The low bound assumes** a round's analyzers cost little next to recompiling the changed projects. That holds for most
  steps here (cheap and expensive analyzers alike took 1-3 s per framework copy after a fix) but not for every analyzer:
  the first analysis, with all of them, is 8-16x the median step.
- **Plain `dotnet format`, no preset** (each rule at its own default), as in the audit's section 3. `bro format` adds the
  preset, the built-in IDE rules and its own plan entries (several processes per run), which multiply these costs rather
  than change their shape.
- **Single machine, shared host.** Newtonsoft.Json's two runs differed by 1.5x in total (above). Shouldly and Serilog
  were steady.
- **Traces of a single id** don't show the loop's context: an id alone pays for building indexes and compilations that,
  in the loop, an earlier id may already have built (or invalidated).

## Next (proposals; the owner decides)

1. **Fix the double guard check in the two rename providers** (above): a small change, a few seconds per rename id per
   process.
2. **Root-cause the Serilog qualifier difference** (above) with a FixOrderTests case from Log.cs.
3. **Phase 3's interaction list:** derive it by testing (merged vs. sequential fixes on the rule test sources) instead of
   reusing FixOrderTests' groups. Those groups make 7-8 chained rounds where the repositories' own edits probably need
   fewer.
4. **The host (phases 1-2) should call each fixer once per id**, not once per project's analyzer reference, and register
   no single-diagnostic action just to read the equivalence key.
