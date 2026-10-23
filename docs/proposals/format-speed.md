# Proposal: a faster `bro format`

Status: **proposal, for the owner's review** (owner's decision 2026-10-17: "Yes, after this fix round": a design doc
first, measured on the trial repositories; implementation later, in separate PRs). Phase 0 is implemented; phases 1 (the
host for checks) and 2 (the host for fixes: fixed order, each fixer once per id, re-analysis only of what a fix changed;
`FixLoop.cs`, `AnalysisCache.cs`, `DiskState.cs`) are implemented behind `--engine host` (`src/StyleBro.FormatHost`,
`src/StyleBro.Migrate/FormatHost.cs`, docs/format-engine.md, with phase 2's measurements), with the parity harness
`scripts/format-speed/Compare-Format.ps1`. Phase 3 waits (owner's decision 2026-10-23,
[checks before merged fix rounds](https://github.com/bisforboman/stylebro/blob/main/docs/decisions/2026-10-23-the-format-host-works-on-its-checks-before-merged-fix-rounds.md)):
measured after phase 2, the checks were the largest cost, so the host's checks now share its analysis cache (keyed by a
project's content), and a command's runs keep their workspaces loaded.
`format --verify-no-changes` and `--diff` (preview) share format's run plan, so everything below applies to them too.

## Summary

`format`'s first run takes 10-50x a build and 2-15x a plain `dotnet format` (trials 26-27). Measured on three trial
repositories (tables below), the time goes to four things, in this order:

1. **Fixed costs repeated per `dotnet format` process.** Every plan entry (framework, configuration, shared-file part)
   is its own process, and in most repositories each entry is two processes (style pass + analyzer pass), started
   three times per run (check, fix, re-check). Each process starts the CLI and MSBuild, loads the workspace and
   compiles everything again: CliFx starts 24 `dotnet format` processes for 1.5-3 s of analysis each.
2. **dotnet format's own fix loop:** one Fix All per diagnostic id, with the analyzers for that id run again over every
   project before each one. On PalCalc a single plain `dotnet format` spends 85-95 s of 160-173 s in that loop
   (about 50 ids at ~1.8 s each).
3. **Convergence runs that reload everything for a few files.** PalCalc's second run changed 2 files and took 162 s
   (88 s checking all three configurations, 74 s fixing 5 files, 26 s of that loading the workspace and 20 s compiling
   it); the third run, which changed nothing, took another 91 s.
4. **Planning:** `dotnet msbuild` evaluations, 2-6 s each, some of them one at a time, and a `dotnet restore` of an
   already restored solution. PalCalc: 76 s before the first `dotnet format` starts; for `--verify-no-changes`, half
   of its 191 s.

The recommendation is to **replace the `dotnet format` child processes with an in-process host** that loads each
configuration's workspace once (MSBuildWorkspace from the SDK's own Roslyn), keeps it for the check, every fix, every
run and the re-check, applies the Fix All providers itself in a fixed order, and re-analyzes only what changed. It
comes after a round of quick wins in today's architecture (planning, restore, verify's cache) that need no new engine.
Target: first run within ~3x a plain `dotnet format` of the same state, re-check within ~1.5x a plain
`dotnet format --verify-no-changes`. Open questions for the owner are at the end.

## How a run works today

`FormatCommand.RunOnce` builds a plan, then `Repeat` runs it until a run changes nothing (at most 3 runs):

- **Planning.** `dotnet sln list`, one `dotnet msbuild -getProperty` per project (`ReadProjects`), one more per
  (project, framework, configuration) for its `#if` symbols, language version and compile items (`CompileSettings`),
  `dotnet restore`, building in-solution generators (`BuildGenerators`). From that: one entry per framework
  (`Plan`/`MergeFrameworks`), per configuration whose `#if` code differs (`ConfigurationRuns`), split into parts where
  projects compile a shared file differently (`Separate`, roslyn#86001).
- **Per entry**, a temporary `.slnf`, `TargetFramework`/`Configuration` set as environment variables plus the
  `SelectFrameworkTargets` hook (projects that don't target the framework load as usual), `--include` the entry's files,
  `--diagnostics` the 141 ids. One process per pass: a plain `dotnet format`, or `style` + `analyzers` where IDE0055 is
  off (CSharpier repositories, most `init` setups), or `whitespace` + `style` + `analyzers` where it's off in some
  folders (`Passes`).
- **Check first.** With more than one entry, every entry runs `--verify-no-changes` at once (at most one per
  processor), then the entries with findings run one after another on only the files with findings, unless a run
  before them touched their files (`AfterCheck`/`Touched`: then they run as usual).
- **After a run**: unused usings (IDE0005) in an extra run where enforced, `DocIndentation`, CSharpier on the changed
  files, `IfBodies` putting back protected indentation, snapshots and the kept findings (an environment-variable file
  the analyzers' fixers write). A run that changed files is followed by another run, which checks everything again.
- **Verify** is the same plan with `--verify-no-changes`; when it finds something it formats a copy (`PreviewCommand`)
  to tell kept findings from real ones, unless `SaveKept`'s record for the same state (hash of every C#, project and
  settings file, the options and the tool version) lists them.

Everything around the child processes reads their console output: finding lines (`IsFindingLine`, `FindingPath`,
`AsKept`, `IfBodies.IsKeptFinding`), fix failures, crashes, skipped projects. That is the seam any new engine plugs into.

## Measurements

Setup: `bro` and StyleBro.Analyzers `0.8.0-trial27` (the trial's packages, main 12961da1), SDK 10.0.402,
Windows 11, 16 logical processors. Each repository is a copy of its trial clone under `.trials/speed/`, reset to the
trial's step-4 commit (after `init --write` and the package, before the first `format`). Every `dotnet` call the tool
made went through a logging shim on `PATH` (start, duration, arguments; a 30-line console app named `dotnet.exe` that runs the real one, not committed), and `format` ran with
`--verbosity diagnostic`, which passes dotnet format's own timers through ("Loading workspace ... Complete in N ms",
"Running N analyzers on <project>"). **Single runs on a shared machine:** other agents ran trials at the same time
(CPU at 100 % with ~60 `dotnet` processes during the io-abstractions run, ~33 % at the start); two identical CliFx
runs took 155 s and 133 s. Compare within a table, not across days; absolute times run up to ~2x the trials'.

### Totals

| Repository | Projects / frameworks / configurations | Plan entries | `format` first run (runs) | Plain `dotnet format`, same state | Plain verify, clean state | `format --verify-no-changes`, clean state | Build (incremental / `--no-incremental`), clean state |
|---|---|---|---|---|---|---|---|
| CliFx | 6 / 5 / 1 | 6 | 133-155 s (2) | 26.6 s | 49 s | 160 s | 29 s / 53 s |
| PalCalc | 15 / 1 / 3 | 3 | 644 s (3) | 173 s | 75 s | 191 s | 66 s / 118 s |
| System.IO.Abstractions | 11 / 7 / 2 | 15 | 574 s (2) | 271 s | 159 s | 174 s | 193 s / 113 s |

The builds ran under the heaviest load (the incremental build slower than the full one shows how much); the trials'
own build times, on a quieter machine, are in the next paragraph. Plain verify exits 2 on CliFx (it reports the
whitespace and other analyzers' findings `format` leaves alone on purpose), 0 elsewhere.

Trials 26-27 for comparison (their own reports, other days): PalCalc 447 s (build 8-22 s, plain `dotnet format` 29 s,
on the formatted code), NLog 13 min (now ~6 min; build 10 s), Lua-CSharp 8.5 min (build 16 s, plain fix 1m40), Serde
7.5 min (build 3 s), CliFx 177 s (build 9-16 s, 38 s), System.IO.Abstractions 346 s (plain 149 s).

Note the baseline: a plain `dotnet format` of the **same unformatted state** is itself slow where there is a lot to
fix (PalCalc 173 s, 477 files): its per-id loop dominates. The trials' 29 s for PalCalc is plain `dotnet format` on
code that was already formatted, which is the right baseline for a re-check, not for a first run.

### Where a first run goes

CliFx (133 s run with timers; 6 entries: net10.0 in 2 parts, net6.0, net7.0, netstandard2.0, netstandard2.1; IDE0055
off, so two passes per entry):

| Phase | Wall | Processes | Inside |
|---|---|---|---|
| Tool start, `sln list`, 15 evaluations (parallel), restore, generator build | 10 s | 18 | evaluations 1.5-1.9 s each, restore 1.8 s |
| Run 1 check, all entries at once | 26 s | 12 | each 9-13 s under contention |
| Run 1 fixes, one entry after another | 68 s | 12 | per process ~5.6 s: start ~0.5-2 s, workspace load 2.3-4.5 s, files 0.2 s, analysis + fix 1.5-3 s |
| CSharpier on 11 files | 1 s | 1 | |
| Run 2 check (clean) | 27 s | 12 | |

All 24 `dotnet format` processes together: 320 s of process time, of which dotnet format's own timers account for
183 s (workspace loads 97 s, determining files 12 s, analysis and fixes 74 s); the other 137 s is process start, the
CLI, MSBuild setup and JIT outside those timers (it grows under contention: ~1-2 s per process alone, up to ~9 s when
12 start at once). **Analysis and fixes are under a quarter of the time.** One plain `dotnet format` of the whole
solution: load 5.0 s, analysis 12.8 s, fixes 6.6 s (7 ids), 26.6 s in all.

PalCalc (644 s; 3 entries: default, Release (4 projects), Release_NoBundle (1 project); whole-run passes):

| Phase | Wall | What |
|---|---|---|
| Planning | 76 s | 16 project evaluations (5-6.5 s each, parallel), 3 + 9 framework/configuration evaluations (parallel), **11 evaluations one at a time** (09:41:26-09:42:02, ~3 s each: Release and Release_NoBundle views of projects the plan reaches through references, asked for while the plan is built, outside the parallel prefetch; the exact call site is for the implementation PR), 11 more (parallel), `dotnet restore` 17.5 s of an already restored solution |
| Run 1 check | 83 s | 3 processes at once (44, 53, 78 s) |
| Run 1 fixes | 227 s | default 162 s (load 21.5 s, files 3.2 s, code-style analysis 27 s + fixes 5 s, analyzer analysis 16 s, **analyzer fixes 85 s**), then Release 33 s and Release_NoBundle 29 s, which fix the same shared files' `#if` variants |
| Run 2 check + fix (2 files changed) | 88 s + 74 s | the fix of 5 files: load 26 s, code-style analysis 20 s (compiling the solution), analyzer analysis 8.5 s, fixes 11 s |
| Run 3 check (clean) | 91 s | |

The default configuration's fix process (162 s) costs about what plain `dotnet format` of the whole solution costs
(173 s): dotnet format's own pipeline is the floor of today's design, and everything else (planning, three checks,
the other configurations, a second fix run) comes on top, 4x in all.

System.IO.Abstractions (574 s; 7 frameworks, a Release configuration, shared-file parts: 15 entries; machine at 100 %
CPU throughout, so every number here is inflated, the shape is what counts):

| Phase | Wall | What |
|---|---|---|
| Planning | 41 s | 99 `dotnet msbuild` evaluations (376 s of process time, 32 s wall), restore 6.7 s |
| Run 1 check | 80 s | 15 entries at once (14-78 s each) |
| Run 1 fixes | 318 s | 8 entries with findings, one after another: net10.0 87 s, net472 40 s, net6.0 22 s, net8.0 86 s, net9.0 19 s, netstandard2.0 19 s, netstandard2.1 21 s, net8.0 Release 24 s; mostly the **same library files** fixed again for each framework (113, 110, 55, 113, 8, 55, 55, 3 files with findings) |
| Run 2 check (clean) | 127 s | 15 entries at once |

Over both runs: 30 `dotnet format` processes; their workspace loads add up to 448 s (15 s each under this load), the
analysis and fixes to 556 s. The plain `dotnet format` of the whole solution, same state, same load: one load of 40 s,
271 s in all.

`--verify-no-changes` on the clean state, for comparison: CliFx 160 s, of which planning 19 s, checks 38 s, and then
**99 s of two `dotnet build -c Release --no-incremental`** (`UncheckedLine`: the configurations an in-solution generator
keeps `format` from checking are built, one after another); io-abstractions 174 s (planning 81 s, of which evaluations
58 s wall and restore 21 s; checks 90 s); PalCalc 191 s (planning 95 s, checks 90 s).

### dotnet format's fix loop

`dotnet format` (SDK 10.0.402: `DotnetTools/dotnet-format`, Roslyn 5.9) runs, per pass: load, determine files, run
all analyzers once, then **for each diagnostic id with findings**: run the analyzers that report it again over every
project of the workspace, get its fixer's Fix All, apply. PalCalc's plain run logged 266 "Running N analyzers on
<project>" lines; the analyzer fix phase was ~50 id iterations, 0.2-4 s each (mean ~1.8 s), 95 s. The order of the ids
varies between processes (CLAUDE.md, "Convergence"); `FixOrderTests` asserts that every order of the interacting ids
gives the same text.

### Re-checks and verify

- A run after a run that changed files checks every entry again: CliFx 27 s, PalCalc 88-91 s per re-check, even when
  the previous run changed 2 files.
- `--verify-no-changes` right after a clean `format`: PalCalc 191 s, of which **95 s planning** (evaluations 71 s wall,
  restore 16 s) and 90 s checks; plain `dotnet format --verify-no-changes` 75 s. `SaveKept`'s record matched (no copy
  was formatted), but only after the checks had run: the record is used to classify findings, not to skip the checks.

## Options

Each option: expected gain (from the measurements), risks, effort, and how to show the results stay the same. "Same
results" throughout means: byte-identical working trees after `format` (every run), the same kept findings with the same
reasons, the same exit codes and the same stylebro.baseline, on the speed copies of the trial repositories (CliFx,
PalCalc, System.IO.Abstractions, NLog, Lua-CSharp, Serde, splat), plus the real-world CI jobs.

### Quick wins in today's architecture (no new engine)

**Q1. Planning in one parallel wave.** Prefetch every (project, framework, configuration) view `ConfigurationRuns` and
`Separate` will ask for, not only the test projects', so no evaluation runs alone; read `WarningsNotAsErrors`,
`Configurations`, `Platforms` in the first per-project evaluation (`ProjectQuery`) instead of separate calls. Gain:
PalCalc ~35 s of 76 s; nothing on CliFx. Risk: none to results (same evaluations, other order). Effort: small.
Proof: the plan lines (`Multi-targeted`, `Configurations`, `Shared files`) and the runs' arguments identical.

**Q2. Skip the restore when the restore output is current.** `dotnet restore` of an already restored solution took
17.5 s on PalCalc (1.8 s on CliFx). Skip it when every project's `project.assets.json` is newer than its project file,
the props/targets it imports, `Directory.Packages.props`, `nuget.config` and `global.json`, and lists StyleBro.Analyzers
(the trial-27 check `LoadsStyleBro` already reads it). Risk: a stale restore (a package changed elsewhere) gives the old
analyzers; NuGet's own no-op check is more exact than file times. Effort: small. Proof: same results; a test for each
"must restore" case (just added the package, edited a props file). **Owner decision Q-D4.**

**Q3. Verify from the clean record.** When `SaveKept`'s record matches the current state, `--verify-no-changes` can
answer from it without running anything: "Clean" with the recorded kept findings. Add the SDK version, the
StyleBro.Analyzers version from the assets files and the `dotnet format` version to the key. Gain: PalCalc's verify
191 s -> a few seconds (hashing the files) after a `format` on the same machine; CI (fresh checkout, no record) gains
nothing. Risk: a cache that says clean when it isn't: whatever the key misses (a package's content changed under the
same version, an environment variable that changes MSBuild evaluation). Effort: small. **Owner decision Q-D5.**

**Q4. Re-check only what a run could have changed.** After a run, an entry none of whose files changed (and whose
projects don't reference a project with changed files) can replay its last check. Gain: small where every entry shares
the changed files (PalCalc, CliFx: all entries compile the same library), larger in repositories with independent
projects. Risk: a fix reaching files outside its entry (renames update references solution-wide): the "changed" set
must be all files changed, which `Snapshot` already gives. Effort: small to medium. Most of this option's value comes
for free with the host (C3).

**Q5. Two passes in one process.** Not possible with `dotnet format` as it is: a plain run always includes the
whitespace pass, so where IDE0055 is off the style and analyzer passes need two processes (`Passes`). Listed because it
is half of CliFx's processes; the host removes it.

Together Q1-Q3 take PalCalc's verify from 191 s to seconds (same state) or ~120 s (another state), and its first run
from 644 s to ~590 s. They don't touch the structure: the run itself stays 4x a plain `dotnet format`.

### (a) An in-process host instead of `dotnet format`

`bro` loads the workspace itself and runs analyzers and fixes in its own process:

- **One MSBuildWorkspace per configuration** (default, Release, ...), loaded once per `format` call and kept for the
  check, every fix, every run and the re-check. A multi-targeted project loads once per framework in that workspace
  (as plain `dotnet format` loads it: `CliFx(net6.0)`, `CliFx(net7.0)`, ...), so frameworks need no reload.
- **Plan entries become solution views**: for an entry, a `Solution` with only its projects (the others removed;
  Roslyn snapshots are immutable and share unchanged compilations), its `--include` documents and its ids. The runs per
  framework stay one after another, as today; after an entry's fixes, the new text of each changed file is written into
  every linked copy of it (all frameworks' documents of that file) before the next entry, which is what today's runs
  see on disk. Roslyn's linked-file merge (roslyn#86000/#86001, the reason for the parts) never runs: no fix sees two
  copies of a file at once.
- **The fix loop is ours**: diagnostics for the entry's ids, then per id (fixed order, Q-D2) the fixer's Fix All on the
  current solution, the id's analyzers again (on the documents that changed, see c). The same formatters dotnet format
  runs: the whitespace formatter (`Formatter.FormatAsync` with the document's options) where IDE0055 applies, the
  code-style analyzers and fixers of the SDK for IDE ids, the analyzer references' fixers for the rest.
- **Output**: the same finding lines (`path(line,col): warning BROxxxx: message`, `WHITESPACE: ...`) through the same
  `Watch`/`Show` callbacks, so `Repeat`, `AfterCheck`, kept findings, `IfBodies`, `DocIndentation`, CSharpier and the
  baseline code stay as they are. The kept-finding file stays an environment variable (the analyzers' fixers read it).

Expected gain, from the measurements:

| | Today | With the host (estimate) |
|---|---|---|
| CliFx first run | 133-155 s: 24 processes, 1 load each | ~35-45 s: planning 10 s, 1 load 5 s, analysis ~13 s, fixes ~7 s, re-check of the 11 changed files ~3 s |
| PalCalc first run | 644 s | ~250-330 s: planning ~40 s, 3 loads ~60 s, analysis ~45 s, fixes ~90 s (dotnet format's loop, b halves it), re-checks incremental ~20 s |
| PalCalc re-check run (2 files) | 162 s | ~10-20 s (no load, re-analysis of the changed documents) |
| PalCalc verify, other state | 191 s | ~90-100 s (planning ~40 s, loads ~60 s, analysis) |
| System.IO.Abstractions first run | 574 s (machine fully loaded) | ~200-280 s: 2 loads (default, Release) ~80 s instead of 30 (448 s summed), analysis of 15 entries, fixes ~the plain run's loop, incremental re-check |

Risks:

- **Result changes through what a run loads.** Today an entry's `dotnet format` sees only its projects; guards and the
  renamer's solution-wide checks (`RepositoryNames`, string guards, `KeptReason.UsedInProjectNotLoaded`) were built for
  that. The solution views keep exactly that view; a host that analyzed the whole workspace instead would change
  renames. The views must be tested for it (the trial-6 Scrutor case, trial-21 splat's BRO1311 retry).
- **`TargetFramework` as an environment variable.** `SelectFrameworkTargets` works because an environment property can
  be overridden by the hook (`<TargetFramework></TargetFramework>` for projects that don't target the run's framework); a
  global property passed to `MSBuildWorkspace.Create(properties)` can't be. Loading every framework at once (above)
  removes the need for it, since every project instance has its own framework. Still to verify: that the per-framework
  project instances MSBuild produces resolve references the way the env-var runs do (nearest framework; `Keep`/`Other`/
  `Aliases` exist for the cases where they didn't), and how Roslyn 5.x's out-of-process BuildHost passes environment
  and global properties (one BuildHost per workspace or shared).
- **Configurations still need a workspace each**: `Configuration` changes DefineConstants, references and compile items.
  A later step could derive a configuration's projects from the default's with other parse options when `CompileView`
  shows only the symbols differ (saves a load per configuration; not in the first version).
- **Fix order.** dotnet format's order is per process and not stable; a fixed order is deterministic, and
  `FixOrderTests` already asserts that interacting ids give the same text in every order. Where a pair isn't covered,
  the parity runs will show it (and add a FixOrderTests case).
- **IDE fixers.** The SDK's code-style fixers (IDE0005, IDE0011, IDE0055's formatter, ...) come from
  `Microsoft.CodeAnalysis.CSharp.Features` and `Sdks/Microsoft.NET.Sdk/codestyle/cs/*.Fixes.dll`; dotnet format finds
  them with MEF. The host has to load the same assemblies the same way, and behave like dotnet format where they
  misbehave (fixers without Fix All: "Unable to fix CS0103 ...", which `FixFailure` reads).
- **Memory.** One process holds every framework's compilations of a configuration: NLog (7 frameworks, ~340 files each)
  or splat (41 projects, 12 frameworks) may need several GB. Mitigation: drop an entry's compilations once the next
  framework's runs start (keep the syntax trees), measure on splat.
- **The dotnet format behaviours format relies on**, re-done: `--include`/`--exclude` globbing, `.slnf`, the report
  (`--report` JSON, `--json`), `WarningsNotAsErrors` handling (dotnet format skips those rules), the whitespace pass on
  files outside any project, generated-code detection, `--severity`, workspace load warnings (`Explain`), crash
  reporting. Each has a test today; each needs one against the host.
- **`DirectiveMask`** swaps `#` on disk while the children run and recovers through tracked child PIDs: with the host it
  can work on the in-memory texts instead (no files to put back after a kill), a simplification, but a change.

Effort: large (several PRs, see the phases). Proof: the parity harness (below) on every trial copy, byte-identical; the
host behind an option until it is.

**Which Roslyn and MSBuild.** StyleBro builds against Roslyn 4.8 (so the analyzers load in every SDK and Visual
Studio); `bro` references only `Microsoft.CodeAnalysis.CSharp` 4.8. A host on Roslyn 4.8 Workspaces is
ruled out: 4.8 can't parse C# 14 (CLAUDE.md, BRO1144), and the trial repositories target net10.0. Shipping a newer
Roslyn in the tool is ruled out too: the SDK's code-style analyzers are built against the SDK's compiler (5.9 for
10.0.402) and don't load in an older one, and a newer SDK would always be ahead of what the tool shipped. So the host
**uses the SDK's own Roslyn**, as dotnet format does:

- MSBuildLocator registers the SDK that `dotnet --version` resolves in the repository folder (its `global.json`), the
  same SDK `dotnet format` would run;
- an `AssemblyLoadContext` resolves `Microsoft.CodeAnalysis*`, `Microsoft.Build*` and their dependencies from that
  SDK's `DotnetTools/dotnet-format` folder (Workspaces.MSBuild, Features, BuildHost-netcore/net472 are there), and the
  code-style analyzers from `Sdks/Microsoft.NET.Sdk/codestyle/cs`;
- the host code compiles against the 4.8 reference assemblies and uses only API present there (Roslyn keeps binary
  compatibility; `MSBuildWorkspace` is in Workspaces.MSBuild 4.8 too); anything newer through reflection, as
  `CSharp14.cs` does for syntax;
- StyleBro's analyzers and fixers load into that Roslyn exactly as they do in the compiler and in dotnet format today;
- **two Roslyns in one process**: the tool already uses Roslyn 4.8 in its default load context (`IfBodies` parses,
  `Conventions` compiles trees, `ConfigSet` builds an `AnalyzerConfigSet`, the descriptors come from the referenced
  StyleBro.Analyzers). A `Solution` from the host's context and a 4.8 `SyntaxTree` don't unify, so the host lives in its
  own context and crosses the boundary with plain types only (paths, texts, finding lines: the seam above), or the whole
  tool resolves Roslyn from the SDK at startup (a bigger change);
- **the runtime**: the SDK's dotnet-format assemblies target the SDK's framework (net10.0 for 10.0.x); the tool is net8.0
  with `RollForward=Major`, which moves to a newer runtime only when .NET 8 is missing (found in phase 1: with .NET 8
  installed the tool ran on it). With `--engine host` the tool runs itself again once on the newest installed runtime
  (`DOTNET_ROLL_FORWARD=LatestMajor`), and the host checks the runtime before loading and falls back to the child
  processes (saying why) otherwise.

To verify before building: dotnet format's source (dotnet/sdk `src/BuiltInTools/dotnet-format`) for how it discovers
fixers, builds its Fix All contexts and orders ids, and which of its pieces are public enough to reuse.

### (b) All ids' fixes in one pass, where they don't interact

Instead of one Fix All per id with a re-analysis before each, compute all diagnostics once, then apply every id's fix in
one round: per document, the text changes of all ids, merged when they don't overlap; ids whose fixes interact (the
FixOrderTests pairs: braces + blank lines, regions + ordering, renames with anything) run in their own rounds, renames
last. Repeat rounds until nothing is reported. Gain: dotnet format's loop on PalCalc is ~50 iterations, 85-95 s; a few
rounds of one analysis each (~15-20 s on PalCalc) would take ~40-50 s off a first run. Needs the host. Risks: the
largest of all options for results: today each fix sees the previous fixes' output, here a round's fixes see the same
input, so overlapping or neighbouring edits (blank lines next to braces, a moved comment) can come out differently, or
need one more round; "identical in every order" is asserted only for the FixOrderTests pairs. Effort: medium on top of
(a), plus a list of interacting ids that must be kept current (a test could derive it: two ids interact when applying
both merged differs from applying them in sequence, on the rule test sources). Proof: byte-identical parity on the
trial copies; any difference becomes a new "interacting" pair.

### (c) Incremental re-checks

With the workspace in memory, a re-check analyzes only the documents a fix changed, plus documents whose diagnostics can
depend on them: same-project documents for semantic rules that look across files (renames, BRO1601 inheritdoc,
BRO1409), nothing for syntax-only rules. Analyzers registered as compilation-end or symbol actions (FieldNamingAnalyzer)
need the whole compilation, which Roslyn rebuilds incrementally (unchanged trees are reused, binding isn't). Gain:
PalCalc's second and third runs (162 s + 91 s) -> ~10-30 s; CliFx's re-check 27 s -> a few seconds. Risk: missing a
diagnostic that a change caused elsewhere; a "full" check stays the last step of `--verify-no-changes` and of a run
said to be clean (or the analyzers declare which rules are document-local, which they already know: syntax-tree actions
vs semantic). Effort: medium; needs (a).

### (d) Caching across invocations

- The clean record (Q3) for verify.
- Planning results (the evaluations) keyed by the hash of every project, props, targets and `global.json` file: a second
  `format` or a `verify` right after skips the 40-95 s of evaluations. Risk: an evaluation depends on more than those
  files (environment variables, SDK version: put them in the key). Effort: small.
- Compilations across invocations: not proposed. Roslyn has no supported persistence for compilations outside the IDE's
  workspace service; the real cost (load and bind) is per process.

### (e) Parallelism

- Fix runs stay one after another where entries share files (every measured repository: they all compile one library
  for several frameworks): the second entry must see the first one's text.
- Checks already run at once (one per processor). Under load that is what makes each check 2-3x slower than alone
  (CliFx: 5.6 s per process alone, 9-13 s at once). With the host, a check is an analysis of an already loaded solution:
  `CompilationWithAnalyzers` runs analyzers concurrently inside one process; a semaphore of a few entries at a time
  instead of one process per processor, so the machine isn't flooded (splat: ~100 processes before the limit).
- Entries that share no files and no project references (configurations of unrelated projects) could fix at once; rare
  in the trials, not worth the risk in the first version.

### (f) Cheaper planning

- Q1 (one parallel wave) first.
- With the host, evaluations run in process through MSBuild's API (`ProjectCollection`, one evaluation ~0.1-0.5 s
  instead of a 2-6 s `dotnet msbuild` each); the `AddImplicitDefineConstants` target still needs a build request, also
  in process. PalCalc's 48 evaluations: 51-71 s wall -> a few seconds. Risk: an in-process evaluation sees the host's
  environment (`MSBuildExtensionsPath`, `MSBuildSDKsPath` from MSBuildLocator); results must equal the CLI's, a test
  compares them on the trial copies.
- Generators (`BuildGenerators`) stay a `dotnet build` of the generator projects (they must exist on disk for the
  workspace to load them).

### Also seen

- `dotnet format --verbosity diagnostic` output in `format` is deduplicated (`Show`), which hides per-process timings:
  a `--timings` option (phase durations per entry and run) would make these measurements repeatable for everyone.
- Verify's builds of the configurations a generator keeps from being checked (`UncheckedLine`) run one after another
  with `--no-incremental`: CliFx 99 s of a 160 s verify. One `dotnet build` of those projects in that configuration
  (MSBuild builds them in parallel, a shared reference once) and incremental where the output is newer than the sources
  would be a quick win of its own (Q6); the host doesn't change it, since the build is the check.
- The IDE0005 follow-up run (`EnforcesUnusedUsings`) is a whole extra pass over every entry; with the host it is one
  more id in the loop, last.

## How to prove the results stay the same

1. **A parity harness** (`scripts/format-speed/Compare-Format.ps1`): for each repository and commit in a list (the trial
   repositories at their step-4 commits, cached clones), run the current `format` and the new one on two copies, compare
   the trees byte for byte after every run, the kept findings (path, line, id, reason), the exit codes, the baseline and
   the `--json` report; print the time of both. Any difference fails.
2. **Real-world CI**: the CSharpier repositories already run `bro format`; a job that runs the parity
   harness on two or three repositories (one multi-targeted with parts, one with configurations, one CSharpier) on PRs
   touching src/StyleBro.Migrate.
3. **The host behind an option** (`--engine host`) until the harness is clean on every trial copy, then the default
   with `--engine dotnet-format` as the way back for one release, then removed.
4. Existing unit tests of FormatCommand keep working against the line format; new tests per dotnet format behaviour the
   host takes over (include/exclude, slnf, report, WarningsNotAsErrors, load warnings, fixers without Fix All).

## Recommendation

1. **Phase 0, quick wins** (one PR each, low risk): Q1 planning in one wave, Q3 verify from the clean record (key with
   SDK and package versions), Q6 verify's generator-configuration builds at once and incremental, Q2 restore skip if
   the owner agrees, and a `--timings` option. Expected: verify on the state a clean `format` left from 160-191 s to
   seconds, CliFx's verify on another state ~100 s faster, first runs 5-10 % faster.
2. **Phase 1, the host for checks**: the in-process workspace, solution views and finding lines, used for the check
   phase and `--verify-no-changes` only (no fixes yet), with the parity harness comparing the findings. This proves
   loading, the views and the SDK's Roslyn with no risk to the code.
3. **Phase 2, the host for fixes**: our fix loop in a fixed order, linked copies updated between entries, the
   whitespace and code-style formatters, kept findings, then incremental re-checks (c). Behind `--engine host` until
   the parity harness is byte-identical on every trial copy; then the default.
4. **Phase 3, merged fix rounds** (b), only if the measurements after phase 2 still show dotnet format's per-id loop
   as the largest cost, and with interacting ids derived by a test.
5. Then remove the child-process path, the `.slnf` files, the `TargetFramework` environment hook and the parts logic
   it needed, once a release has shipped with the host as the default.

**Targets** (measured with the parity harness on the trial copies, machine otherwise idle): a first run within ~3x a
plain `dotnet format` of the same state, a re-check run (a run after one that changed files) within ~1.5x a plain
`dotnet format --verify-no-changes` of the result, `--verify-no-changes` on the state a clean `format` left within a
few seconds. For CliFx that is ~80 s / ~40 s (today 133-155 s / 27 s per re-check), for PalCalc ~500 s / ~110 s (today
644 s / 162 s), with phase 2's estimate well under both.

## Open questions for the owner

Each is asked as a multiple-choice question; the recommended option is first.

**Q-D1. The engine.**
- *In-process host in bro (recommended)*: one workspace per configuration for the whole `format`; the
  largest gain (CliFx ~3-4x, PalCalc ~2-2.5x, re-checks ~10x); a large change that re-does what dotnet format does.
  Before: 24 processes on CliFx; after: 1.
- *A long-lived host process per plan entry*: still a child process, but one per entry reused for check, fix and
  re-check; smaller change (keeps the env-var hook and parts), about half the gain (frameworks still load apart).
- *Keep `dotnet format`, quick wins only*: no risk to results; first runs stay ~4x a plain `dotnet format`.

**Q-D2. The order fixes are applied in.**
- *A fixed order (recommended)*: deterministic runs (today's order changes between processes); `FixOrderTests` already
  requires the same text in every order for interacting ids; a difference the parity runs find becomes a test case.
  Before: two runs of the same repository may apply ids in different orders; after: always the same order.
- *Mimic dotnet format's order*: closer to today, but that order isn't stable, so "byte-identical" can't be promised
  either way, and the host would have to reproduce an implementation detail of every SDK.

**Q-D3. Which Roslyn the host uses.**
- *The SDK's own Roslyn and MSBuild, loaded at runtime from the SDK `global.json` selects (recommended)*: the same
  compiler, code-style analyzers and fixers `dotnet format` and the build use; works with every future SDK. Costs an
  assembly load context, API limited to Roslyn 4.8's surface, and a fallback to the child processes when the SDK's
  runtime isn't there. Before/after for users: none (same compiler as today's `dotnet format`).
- *Ship a Roslyn version in the tool*: simpler code, but the SDK's code-style analyzers don't load in an older Roslyn,
  and C# newer than the shipped Roslyn doesn't parse: every new SDK would need a tool release first.

**Q-D4. Restore.**
- *Skip `dotnet restore` when the restore output is newer than every input and lists StyleBro.Analyzers
  (recommended)*: before 2-18 s per call (PalCalc 17.5 s), after ~0 when nothing changed; `--restore` forces it. Cost: a
  file-time check can miss a change NuGet's own check would catch (a package replaced under the same version).
- *Always restore (today)*: always right, 2-18 s per call.
- *Restore only with `--restore`*: fastest, but a first `format` right after adding the package would run without it.

**Q-D5. Verify from the clean record.**
- *Answer `--verify-no-changes` from the record when the state matches (recommended)*: before PalCalc 191 s, CliFx
  160 s after a clean `format`; after a few seconds (hashing the files). The key gains the SDK, package and tool
  versions; what it still can't see (an environment variable changing MSBuild evaluation) could make it say clean
  wrongly. CI (fresh checkout, no record) is unchanged.
- *Keep running the checks (today)*: verify always re-derives the answer.

**Q-D6. Rollout.**
- *Behind `--engine host`, then the default once the parity harness is byte-identical on every trial copy, the old
  engine kept for one release (recommended)*: before/after for users only once proven; costs keeping two engines for
  a release.
- *Switch directly in one release*: faster to ship and one engine to maintain, any difference lands on users.
