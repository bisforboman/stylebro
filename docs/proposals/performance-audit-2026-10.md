# Performance audit, October 2026

Status: **measurement report, for the owner's review** (owner's decision 2026-10-22: "Measure first"). Nothing in `src/` was
changed for it. Two fixes are proposed below with measured gains from local, unpushed experiments; the owner decides.

## Summary

- **One real regression, in the fixes, not the analyzers.** BRO1112's fix (remove `#region`s) sorts the members with
  BRO1001's logic, and that sort asks `CamelCaseRenamer.GetNamesInCodeAsync` for the solution's names. The names are
  cached per `Solution` object, but the regions fix asks with a document it has just changed (`document.WithText`), so
  every fixed file misses the cache and scans every document of the solution again. On Newtonsoft.Json (907 files with
  regions, 17 projects incl. target frameworks) BRO1112's Fix All takes **387 s; 15.5 s with the names read from the
  original solution**, same output (diff hash identical, 905 files). That one id is a third of a plain `dotnet format`
  with every StyleBro rule (1,107-1,128 s -> 746 s). BRO1409's fix (off by default) has the same pattern.
- **Analyzers: slow drift, no single regression.** Newtonsoft.Json, compare mode, three alternated runs each: main is
  4-7 % slower than 0.7.0-alpha.1 (default rules +4 %, `--all-rules` +7 %), the same as 0.8.0-alpha.1 (medians 0.97x and 1.02x, single repeats -8 % to +4 %), and
  15 % faster than 0.2.0-alpha.1. Every step stayed under CI's 10 % threshold, so the per-PR check couldn't see it. Most
  of the +4-7 % is DocumentationAnalyzer (+13 %), FieldNamingAnalyzer (+8 to +31 %), TrailingBlankLinesAnalyzer (+24 to
  +52 %) and DocumentationStyleAnalyzer (+33 %). StyleCop.Analyzers 1.2.0-beta.556 takes **4.0-4.1x** StyleBro's time.
- **Per file (the IDE's view): linear.** 8x the file size costs 6.6x (JsonTextReader.cs) and 9.4x (EncodingHelper.cs) the
  time; per file main is ~8 % slower than 0.7, the same drift as above.
- **The solution-wide rename guards don't dominate** once the regions bug is out: on Newtonsoft.Json every Fix All
  together takes 85 s of a 746 s `dotnet format`; 577 s is dotnet format analyzing the solution again before each id.
- **`bro format` is 1.9-2.8x a plain `dotnet format` of the same state** (CliFx 119 s vs 45 s, PalCalc 316 s vs 114 s,
  Shouldly 180 s vs 95 s, ClosedXML 499 s vs 260 s), where the proposal (docs/proposals/format-speed.md) measured 5-6x
  on CliFx and 3.7x on PalCalc. The fix entries, which run one after another, are now the largest phase (48-58 % of a
  first run), then the re-check run (19-21 %). `bro check` on a clean state: 0.5 s from the clean record when there is
  one, otherwise 0.9-3.9x a plain verify (most of that: formatting a copy to tell kept findings from real ones).

## Machine and method

One cloud container: 4 vCPUs (Intel Xeon @ 2.80 GHz, one thread per core), 15 GB memory, Linux 6.18, .NET SDK 10.0.113
(runtime 10.0.13) and 8.0.132, StyleBro `main` at `a92c3b9`, built in Release. Trial 42 ran in another container at
the same time; this one ran nothing else, but the host is shared. The proposal's numbers came from a 16-thread Windows
machine with SDK 10.0.402 under heavy load: compare ratios across the two, not seconds.

Noise: the same comparison repeated three times moved main's total by 5-15 % (default rules 414-483 ms) and single
analyzers by up to 50 % (DocumentationAnalyzer 45-76 ms) between repeats; within one compare run both builds see the
same noise, so ratios held better than totals. The tables give every repeat's range.

- **Analyzer time, whole project**: `scripts/benchmark compare <release> <main> Src/Newtonsoft.Json 10 [--all-rules]`
  (Newtonsoft.Json at the real-world pin `52fa3ae`, 242 files), three times per version and rule set, written by
  `scripts/benchmark/Compare-Releases.ps1` and summarized by `scripts/benchmark/summarize-compare.py`. Releases from
  nuget.org (StyleBro.Analyzers 0.2.0-alpha.1, 0.7.0-alpha.1, 0.8.0-alpha.1; StyleCop.Analyzers.Unstable 1.2.0.556, default
  rules only, as docs/stylebro-vs-stylecop.md did).
- **Per file**: `scripts/benchmark file`, `--all-rules`, 7 runs, two alternated rounds of main and 0.7.0-alpha.1:
  Newtonsoft.Json's 10 largest files with its netstandard2.0 symbols, Jellyfin's EncodingHelper.cs, BaseItem.cs and
  Folder.cs (MediaBrowser.Controller at the real-world pin `305a964`, without its project references). Scaling: copies
  of a file with its namespace block repeated 1, 2, 4 and 8 times (`scripts/benchmark/make-scaled.py`), 5 runs.
- **Code-fix time**: plain `dotnet format analyzers <sln> --diagnostics <every BRO id> --severity warn --no-restore -v
  diag`, StyleBro's Release DLLs added through a `CustomAfterMicrosoftCommonTargets` hook (as `scripts/realworld` does,
  no preset), timed per output line by `scripts/benchmark/time-format.py` (which splits the fix phase into one group per
  id); a sampled `dotnet-trace` of the `dotnet-format.dll` process during the fix phase, read with
  `scripts/benchmark/speedscope-top.py`. Repositories: Newtonsoft.Json (global.json removed: it asks for SDK 10.0.300) and
  shouldly/shouldly `b9b5293` (633 `*.approved.*` snapshot files, trial 37).
- **`bro format` / `bro check` wall clock**: `scripts/benchmark/Measure-Format.ps1` with `bro` and StyleBro.Analyzers
  packed from main (`0.0.1-audit`): a fresh clone, `bro init --write`, the package added where init pinned the preset,
  restore, commit (a trial's step 4); then plain `dotnet format`, `bro format --timings`, commit, `bro check --timings`
  (right after, then with the clean records deleted), `bro check --build --timings`, plain `dotnet format
  --verify-no-changes`, `dotnet build` and `--no-incremental`. Repositories: CliFx `10cbf9b` (the commit the trials'
  Docker test used), PalCalc `66f18a0` (the last commit before trial 27; the trial's own commit isn't recorded; WPF,
  `EnableWindowsTargeting=true`), Shouldly `b9b5293` (trial 37), ClosedXML `057f597` (trial 41). One run each.

## 1. Analyzer time, whole project

Newtonsoft.Json, compare mode, each analyzer's fastest of 10 runs, three repeats (ranges over the repeats):

| Base build | Rules | Base total | main total | main / base (median) |
|---|---|---:|---:|---:|
| 0.2.0-alpha.1 (2026-10-06) | default | 495-517 ms | 414-440 ms | 0.85x |
| 0.2.0-alpha.1 | all | 564-572 ms | 481-484 ms | 0.85x |
| 0.7.0-alpha.1 (2026-10-13) | default | 406-464 ms | 445-483 ms | 1.04x |
| 0.7.0-alpha.1 | all | 475-483 ms | 501-519 ms | 1.07x |
| 0.8.0-alpha.1 (2026-10-19) | default | 438-495 ms | 427-471 ms | 0.97x (-41 to +3 ms) |
| 0.8.0-alpha.1 | all | 523-558 ms | 518-543 ms | 1.02x |
| StyleCop.Analyzers 1.2.0-beta.556 (182 analyzers) | default | 1,749-1,801 ms | 431-436 ms | **0.25x (StyleCop 4.0-4.1x)** |

The 2026-10-05 comparison found StyleCop 3.1-3.4x StyleBro's time, on another machine; StyleBro has gained rules since
and is relatively faster here.

Main's slowest analyzers (default rules, from the StyleCop runs) and their change since 0.7.0-alpha.1 (default rules,
median of the three head/base ratios):

| Analyzer | main | since 0.7 | Note |
|---|---:|---:|---|
| TreeWalk (shared walk of every tree) | 75-77 ms | 0.96x | 47-50 ms in 0.2 (it builds more of the shared arrays now, which the analyzers no longer build themselves) |
| DocumentationAnalyzer | 46-49 ms | 1.13x | 259 reports either way |
| FieldNamingAnalyzer | 31-33 ms | 1.31x (default), 1.08x (all) | the most variable one, as before |
| BlankLineRunsAnalyzer | 21-22 ms | 1.02x | |
| MemberOrderingAnalyzer | 17-18 ms | 1.18x | |
| TrailingBlankLinesAnalyzer | 16-17 ms | 1.24x (default), 1.52x (all) | reports 10 -> 0: standalone comments are allowed by default now |
| CommentTextAnalyzer | 16 ms | 0.98x | |
| ParameterLayoutAnalyzer | 13 ms | 0.77x | |
| NullCheckAnalyzer | 11 ms | 0.97x | |
| ParenthesisPlacementAnalyzer | 11 ms | 0.98x | |
| DocumentationStyleAnalyzer | 10 ms | 1.34x | |
| NullForgivingAnalyzer (off by default) | 39-41 ms with `--all-rules` | 0.99x | the slowest off-by-default rule |

Reports per analyzer are the same in main and 0.8.0-alpha.1 and differ from 0.7.0-alpha.1 only in TrailingBlankLines
(above). No analyzer grew a lot since 0.7: the largest absolute growth is FieldNamingAnalyzer (+3 to +15 ms, noisy) and
TrailingBlankLinesAnalyzer (+5 ms); the rest are 1-5 ms each. Since 0.8.0-alpha.1 nothing moved beyond noise.

**TrailingBlankLinesAnalyzer** (proposal, small): `TrailingBlankLines.GetCheckedBlankLines` calls `IsStandalone` (added
2026-10-14), which reads the comment run's lines above and below as strings, for every `//` comment before the cheap
checks; most comments have no blank line below and need no `IsStandalone` at all. Calling it only when blank lines were
found (it can only return true then) took the analyzer alone from 74-76 ms to 53-59 ms on Newtonsoft.Json and 13 -> 10 ms
on Jellyfin's MediaBrowser.Controller (compare mode, `STYLEBRO_BENCH_ONLY`, reports 0 / 0 in both, which proves little:
the change needs the rule's tests and a mutation entry).

## 2. Per file (the IDE's view)

Fastest of 7 runs, all analyzers, `--all-rules`, sum per file:

| File | KB | 0.7.0-alpha.1 | main |
|---|---:|---:|---:|
| NJ Serialization/JsonSerializerInternalReader.cs | 125 | 37.6 ms | 42.7 ms |
| NJ Linq/JToken.cs | 110 | 27.8 ms | 29.0 ms |
| NJ JsonTextReader.cs | 100 | 23.7 ms | 20.1 ms |
| NJ Converters/XmlNodeConverter.cs | 81 | 32.7 ms | 35.7 ms |
| NJ, average of the 10 largest files | | 20.6 ms | 22.3 ms |
| Jellyfin MediaEncoding/EncodingHelper.cs | 348 | 120.1 ms | 128.8 ms |
| Jellyfin Entities/BaseItem.cs | 103 | 50.8 ms | 58.7 ms |
| Jellyfin Entities/Folder.cs | 78 | 48.8 ms | 50.5 ms |

Summed over the files and the faster of the two rounds: Newtonsoft.Json 201 -> 216 ms (+7 %), Jellyfin 212 -> 232 ms
(+9 %); the largest growths are DocumentationAnalyzer, NullForgivingAnalyzer, FieldNamingAnalyzer, DocumentationStyle,
HasValueAnalyzer and TrailingBlankLines, 1-2 ms each. (The 2026-10-05 table in scripts/benchmark/README.md came from
another machine.)

Scaling (fastest of 5 runs, total per file):

| Copies | JsonTextReader.cs | | EncodingHelper.cs | |
|---:|---:|---:|---:|---:|
| 1 | 100 KB | 29.0 ms | 348 KB | 73.9 ms |
| 2 | 198 KB | 52.4 ms | 695 KB | 187.8 ms |
| 4 | 394 KB | 97.1 ms | 1,389 KB | 328.5 ms |
| 8 | 787 KB | 191.4 ms (6.6x) | 2,778 KB | 698.2 ms (9.4x) |

Linear within noise. Three analyzers grew 16-23x in the all-analyzer run at 8 copies (ListGapsAnalyzer and
ParameterLayoutAnalyzer on EncodingHelper.cs, CallChainAnalyzer on JsonTextReader.cs) but not when timed alone on
EncodingHelper.cs (7.1x, 7.4x, 8.2x): shared costs landing on them, as
the README describes. NestedIfAnalyzer is 10-11x for 8x both ways (13 -> 137 ms alone at 2.8 MB): slightly faster than the
file grows; worth a look if it climbs further, not a problem at real file sizes (7.5 ms at 348 KB).

## 3. Code-fix time under `dotnet format`

Plain `dotnet format analyzers` with every BRO id, no preset (rules at their own defaults):

| | Newtonsoft.Json, main | Newtonsoft.Json, regions fix patched | Shouldly, main | Shouldly, patched |
|---|---:|---:|---:|---:|
| Total | 1,107-1,128 s (two runs) | 746 s | 82.2 s | 82.2 s |
| Start, workspace load, files | 13 s | 13 s | 7 s | 7 s |
| First analysis | 68-72 s | 70 s | 15 s | 16 s |
| Fix phase | 1,024-1,041 s | 661 s | 60 s | 59 s |
| of which analysis before each id | 497 s | 577 s | 38 s | 38 s |
| of which Fix All providers | 527 s, **383-388 s of it one id (BRO1112)** | 85 s | 22 s | 21 s |
| Files changed | 937 | 937 | 244 | 244 (same diff) |

BRO1112 alone (`--diagnostics BRO1112`, BRO1001 on by default, so the fix sorts): 443.9 s, its Fix All 387.1 s; patched
75.4 s, Fix All 15.5 s; the same 905 files with the same diff (md5 of `git diff`).

**What the trace shows** (`dotnet-trace`, 300 s of the fix phase, time on stack summed over threads):
`RegionsCodeFixProvider.FixDocumentAsync` -> `MemberOrderingCodeFixProvider.GetSortChangesAsync` ->
`GetReadByPositionAsync` -> `CamelCaseRenamer.GetNamesInCodeAsync` is 854 s of thread time (the Fix All runs documents in
parallel; the threads that don't collect wait for the ones that do), of which `CollectNamesAsync` 214 s and `RepositoryNames.Collect` 181 s.
`GetNamesInCodeAsync` caches per `Solution` (`NamesCache`, a ConditionalWeakTable); `RegionsCodeFixProvider` calls
`GetSortChangesAsync(removed, ...)` with `removed = document.WithText(...)`, a new solution for every document, so the
names (every string, nameof and identifier of every document of every project, plus the snapshot/XAML scan) are
collected once per fixed file instead of once per Fix All. On a repository without `#region`s (Shouldly) nothing changes.

**Proposed fix**: pass the original solution for the names (`GetSortChangesAsync(removed, ct, namesFrom:
document.Project.Solution)` down to `GetReadByPositionAsync`; the names are positional reads elsewhere in the solution,
which removing regions doesn't change). The experiment was those 6 changed lines. `InternalTypeMethodCodeFixProvider`
(BRO1409, off by default) calls `GetSortChangesAsync(changed, ...)` the same way and needs the same parameter. A test could
count `CollectNamesAsync` calls per Fix All. `bro format` runs one `dotnet format` per framework and configuration, so a
regions-heavy multi-targeted repository pays it once per entry.

**The solution-wide guards otherwise**: with the patch, all 70 Fix All groups of Newtonsoft.Json take 85 s together, the
largest 5.6 s (no id named per group; none stands out). Shouldly (snapshot files, Shouldly's message
guard) spends 21 s in all fixes. The guards' scans aren't where the time goes; dotnet format's own loop is (577 s: the
analyzers run again over all 17 project copies before each of ~70 ids, ~8 s per id), as docs/proposals/format-speed.md
found. That loop is what the planned in-process host (phases 1-2) would replace.

Why nothing caught it: CI's `performance` job measures analyzers only, and the real-world jobs print phase durations but
don't fail on them. A fix-side check is listed under "Next" below.

## 4. `bro format`, `bro check`, `bro check --build`

One run each (seconds); "Plain" is `dotnet format` with the repository's settings and StyleBro's package, on the same
state:

| Repository | Plan entries | Plain `dotnet format` (start state) | `bro format` (runs) | `bro check` right after | `bro check`, no record | `bro check --build` | Plain verify (clean state) | Build incr. / full |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| CliFx (6 projects, 5 frameworks, CSharpier) | 3 frameworks | 45.3 | 119.0 (2) | 29.7 | 28.7 | 69.7 | 33.2 | 18.9 / 17.7 |
| PalCalc (15 projects, 3 configurations, WPF) | 3 configurations | 113.7 | 316.0 (2) | **0.5** | 207.3 | 288.8 | 52.5 | 4.2 / 18.3 |
| Shouldly (multi-targeted, x64/x86 platforms) | 5 frameworks | 95.4 | 180.5 (2) | 40.8 | 66.5 | 169.2 | 44.1 | 3.4 / 10.0 |
| ClosedXML (933 files, 4 frameworks, `#if DEBUG`) | 8 (frameworks x Debug/Release) | 260.3 | 499.1 (2) | 107.2 | 249.1 | 299.5 | 70.4 | 20.5 / 18.3 |

Exit codes: format and check 3 ("Not verified") on CliFx, Shouldly and ClosedXML: SDK 10.0.113's `dotnet format` ignores
the multi-target guard's suppressor, so IDE0049 is held back (by design, CLAUDE.md trial 35). That also means no clean
record is written there, so `check` right after `format` can't answer from it; PalCalc (single framework) ended clean and
its check took 0.5 s. CliFx's check also exits 2 because CSharpier would reformat the Directory.Build.props the script
edited (an artifact of the setup, not of StyleBro). Plain verify exits 2 everywhere: it reports findings `format` leaves
alone on purpose (whitespace where IDE0055 is off, other analyzers).

Against the proposal's baseline (other machine, ratios only):

| | Proposal (2026-10-17) | Now |
|---|---|---|
| CliFx first run / plain `dotnet format` | 133-155 s / 26.6 s = 5-6x | 119 s / 45 s = **2.6x** |
| CliFx verify / plain verify | 160 s / 49 s = 3.3x | 29 s / 33 s = **0.9x** |
| PalCalc first run / plain | 644 s / 173 s = 3.7x (3 runs) | 316 s / 114 s = **2.8x** (2 runs) |
| PalCalc verify / plain verify | 191 s / 75 s = 2.5x | 0.5 s from the record; 207 s / 53 s = 3.9x without it |

The quick wins (#248: planning in parallel, restore skipped when current, verify from the clean record, merged frameworks)
show: CliFx has 3 plan entries instead of 6 and its verify no longer builds anything. The proposal's first-run target
(~3x plain) is met on all four repositories; the re-check target (~1.5x plain verify) only where a clean record exists
or nothing needs a copy formatted.

Where a first run goes now (`--timings`; the checks of one run overlap, the fixes don't):

| Phase | CliFx | PalCalc | Shouldly | ClosedXML |
|---|---:|---:|---:|---:|
| Planning (projects, frameworks, configurations) | 2.6 s | 10.1 s | 4.0 s | 6.3 s |
| Generators' build check | 8.7 s | 0 | 0 | 0 |
| Run 1 checks (at once, wall = slowest) | 24.3 s | 62.3 s | 25.8 s | 53.6 s (8 at once on 4 CPUs) |
| Run 1 fixes (one after another) | 48.4 s (3) | 173.2 s (3) | 106.3 s (5) | 289.3 s (8) |
| Run 2 re-check (clean) | 24.0 s | 64.1 s | 33.8 s | 100.5 s |
| **Share of the fixes / re-check** | **41 % / 20 %** | **55 % / 20 %** | **59 % / 19 %** | **58 % / 20 %** |

- **The fix entries dominate**: each is its own `dotnet format` process with its own workspace load, compilation and
  per-id loop, and several fix the same shared files again (ClosedXML: the library is fixed in 8 entries; the Release
  entries 19-33 s each, for `#if DEBUG` differences).
- **The re-check after a run that changed files** is a full check of every entry (ClosedXML 100 s, to find nothing).
- **`check` without a clean record formats a copy** to tell kept findings from real ones: PalCalc 125 s of 207 s,
  ClosedXML 137 s of 249 s. On SDKs that hold IDE0049 back no run is ever "clean", so this is the common case there.
- **`check --build` builds every solution configuration one after another**: Shouldly 6 builds (Debug/Release x
  AnyCPU/x64/x86) of 15-18 s, 100 s of 169 s; PalCalc 3 builds, 77 s.

## Caveats

- One 4-vCPU machine, a shared host, single runs for section 4. The analyzer tables are three repeats of compare mode;
  read differences under ~5 % as noise.
- The analyzer benchmark compiles Newtonsoft.Json against the .NET runtime only (partial binding), like CI.
- The regions-fix gain and the TrailingBlankLines gain come from local patches that weren't committed; a real fix needs
  tests, the mutation entries and a review of `InternalTypeMethodCodeFixProvider`.
- Section 3 runs plain `dotnet format` without the preset (each rule at its own default) to isolate StyleBro's fixes;
  `bro format` adds the preset and the built-in rules.
- PalCalc is a WPF application built on Linux here (`EnableWindowsTargeting`); the proposal ran it on Windows, at another
  commit.
- `time-format.py`'s per-id grouping reads dotnet format's "Running N analyzers on P" lines; the group count can be off
  by a few (67-70 groups for the same run), the totals aren't.

## Next (proposals; the owner decides)

1. **Fix the regions fix's cache miss** (above): the largest single cost found, 383 s on one repository, a small change.
   Do BRO1409's fix in the same PR.
2. **A fix-time guard for CI**: the real-world jobs already time their phases; failing a job when one id's Fix All
   (time-format.py's groups, or `--timings`) passes a budget, or when the StyleBro-only `dotnet format` is more than N x
   the first analysis, would have caught (1) the day it landed.
3. **TrailingBlankLinesAnalyzer's `IsStandalone` after the cheap checks** (-25 % for that analyzer alone; ~4 ms of the
   ~4-7 % drift).
4. **An absolute-drift check**: CI compares a PR with its base only. A nightly `Compare-Releases.ps1` against the last
   release (or the last N releases), failing above ~10 %, would show drift like 0.7 -> 0.8's.
5. **`bro format`'s structure** (unchanged from the proposal, now with today's shares): the sequential fix entries
   (41-59 %) and the full re-check (19-21 %) are what the in-process host (phases 1-2) removes; nothing in planning or
   the guards is worth more than a few seconds.
6. **`check --build`**: build configurations that differ only by platform (AnyCPU/x64/x86 of the same AnyCPU projects)
   once, or in parallel where projects don't overlap.
