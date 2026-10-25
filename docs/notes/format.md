# `bro format` notes

The `bro format` investigation (per-framework runs, convergence, speed on large repositories) and the xUnit fixes it applies. Moved from CLAUDE.md on 2026-10-23.

## `bro format` (2026-10-03, IDE0055 investigation)

- Plain `dotnet format` with IDE0055 at warning crashes on multi-targeted repos (Roslyn's linked-file merge, no
  StyleBro needed; the style pass alone too; reported upstream: https://github.com/dotnet/roslyn/issues/86000 and https://github.com/dotnet/roslyn/issues/86001). The whitespace pass formats only the first framework's code (https://github.com/dotnet/sdk/issues/56585). With
  `TargetFramework=<tfm>` as an ENVIRONMENT variable MSBuild loads each project as an inner build: no linked copies.
  The env var also reaches referenced projects that don't target that framework (Newtonsoft.Json's net46 tests -> the
  library without net46: load failure, tests silently skipped, exit 0); a hook (`BeforeMicrosoftNETSdkTargets`, right
  after the project file, before the SDK's multi-targeting decision) clears it for those. Setting the framework in that
  hook instead does NOT work: the project file's own `'$(TargetFramework)' == 'net46'` conditions (DefineConstants)
  were already evaluated without it.
  Clearing it for EVERY project without the framework broke the other direction: Serilog's TestDummies
  (netstandard2.0) references Serilog, which then offered only net8.0, so TestDummies and the tests using it didn't
  load. Final rule (`FormatCommand.Keep`): keep the forced framework for projects that target it and projects that
  reference one of those (transitively); clear it for the rest. Plus `--include` with the run's project folders
  (TRAILING SLASH: `src/Lib` silently matches nothing), else referenced multi-targeted projects get formatted and their
  copies merged (conflict markers). Final measurements: Serilog one run, nothing left; Newtonsoft.Json one run + a
  second run touching 1 file (braces in net20-only LinqBridge.cs), only guard-held renames left.
  `FormatCommand`: `dotnet msbuild -getProperty:TargetFrameworks/-getProperty:TargetFramework` per project (`dotnet
  sln list`), restore once, then per framework `dotnet format <temp .slnf> --no-restore <options>` (the filter lists
  the projects that target it; works for .sln and .slnx). Measured: Serilog IDE0055 505 -> 0; with every StyleBro rule
  + init: 183 files, second run 0, builds. `init` writes IDE0055 at warning everywhere now and prints a hint for
  multi-targeted repos. Limit: per-framework indentation conflicts (Newtonsoft.Json's `else` inside `#if`).
- Scratch scripts: `scratchpad/ide0055-*.ps1`, `try-format*.ps1`. Gotcha: an incremental build re-reports no warnings
  (use `--no-incremental` when counting).
- Trial 6 (2026-10-11): a per-framework run loads only the projects that target it, so the renamer's solution-wide
  guards missed the others (Scrutor: a field the tests read with GetField renamed in the library's netstandard2.0 run).
  `format` now writes every C# file's strings, nameof names and identifiers (`RepositoryNames`, env
  `STYLEBRO_REPOSITORY_NAMES`) for those runs; the renamers check them (strings like their own, and a non-private
  name used in a file the run doesn't load: `KeptReason.UsedInProjectNotLoaded`). Framework spellings share a run
  (`NormalizeFramework`, NuGet's short names: `net6` = `net6.0`, `net10` is .NET Framework 1.0); the run's
  TargetFramework is the majority spelling, and the hook sets a kept project's own spelling (`StyleBroFormatAliases`).

- **Speed on large multi-targeted repos** (2026-10-14, trial 21's splat: 41 projects, 12 frameworks; format 101 -> 11 min,
  verify 56 -> 2 min, preview 111 -> 12 min, same files, content and kept findings). `Separate` groups projects that compile a shared
  file with the same DefineConstants/LangVersion/Nullable for the run's framework (`CompileSettings`, one evaluation per
  project and framework, in parallel, cached): same parse, same edits, which Roslyn's merge takes; other or unknown
  settings still run apart (kubernetes-client's Aot). Per run: no whitespace pass when the whitespace exclusions cover its
  `--include`, one plain call when they touch none (`Passes(exclusions, include)`); `--include` with named files runs only
  the plan entries whose projects have them (`RunInclude`). The check after a run that changed files counts the kept
  findings of the last run (`Remember`, matched by file + rule + message, `AsKept`) as clean. A clean `format` records its
  kept findings with a hash of every C#/project/settings file + options + tool version (`SaveKept`, %TEMP%/stylebro-kept);
  `--verify-no-changes` on that state needs no copy; otherwise its copy run gets `--include` the finding files and
  `--diagnostics` their ids (`Relevant` drops passes none of them is in). Parallel checks: at most ProcessorCount, with
  `Checked ...` progress. Found on the way: grouping changed BRO1311's result (a name blocked in Splat.SimpleInjector kept
  Splat.Core's unrelated tuples); `TupleElementRenamer` now retries each project's elements on their own
  (`ProjectFiles`), which gave the split runs' result again.
- **Convergence: one run everywhere** (2026-10-04). `dotnet format` fixes one id at a time (one Fix All each, fresh
  diagnostics) in an order that changes between processes, so the same repo converged in one run or two. Causes found:
  (1) BRO1514-BRO1516: braces on one clause of an if/else chain make the others BRO1516's, which may already have run
  (Newtonsoft.Json's net20-only LinqBridge.cs: `if (!e.MoveNext()) yield return x; else do { ... } while (...);`);
  the fix now wraps every finding of the chain. (2) BRO1112 + BRO1001: BRO1001 sorts within regions, so removing them
  left a sort for the next run (FFMpegCore, Newtonsoft.Json's samples); the regions fix now sorts when BRO1001 is on, as
  edits of each sorted container's span: a whole-text edit lost the linked-file merge to the copy where the code is
  inside an inactive `#if` (it only removes the header's `#region License`, an edit at offset 0). (3) BRO1112/BRO1113 +
  BRO1506: `// output` + `#endregion` + blank line left a blank line below a comment (528 in Newtonsoft.Json's samples);
  the regions fix removes it when BRO1506 is on. (4) Regions inside an inactive `#if` (Roslyn parses them, IsActive
  false) came out as BRO1112 in that copy; when BRO1112 ran before BRO1113, that copy's plain removal won the merge and
  skipped (3) (11 Newtonsoft.Json samples). Inactive regions aren't reported now. Found by bisecting ids on one file
  (`dotnet format ... --include <file>` from the repo root) and logging each copy's edits in LinkedFileFixAllProvider.
  (5) Removing regions can create BRO1504's case (`using ...;` + `#region License` + header comment: the comment now
  follows code) and BRO1518's (blank, `#endregion`, blank, `}`); the regions fix inserts / removes those blank lines
  too. These showed up in 1 Newtonsoft.Json run of 5: a single clean real-world run proves little, repeat it.
  `FixOrderTests` runs every order of a case's ids, one Fix All each (single copy: multi-copy cases need a real repo).
  Result: every reference repo needs 2 runs (one that changes, one clean), so Invoke-RealWorld now fails above 2.
- **`#region` lines some frameworks leave out** (2026-10-23, owner's choice after PR #291 saw it). `bro format --engine host`
  on Newtonsoft.Json (init --write + package, as a trial's step 4) ended "Not clean: each run undoes the one before in 1
  file": `Src/Newtonsoft.Json.Tests/Linq/DynamicTests.cs` is `#if !(NET35 || NET20 || PORTABLE40)` throughout, and its
  `#endregion` lines right below an inner `#endif` (464, 538, 612, 686) went to column 0 in the net20/net35 runs and back to
  their indentation in the others. Roslyn's formatter re-indents a `#region`/`#endregion` below another directive inside
  a left-out branch from the next compiled token (column 0 there; a region below left-out text stays, `#pragma`,
  `#nullable`, `#error` are column 0 in every parse); this only differs from the active copy where the left-out branch
  spans a block boundary (a probe with the `#if` inside one method gave the same indentation both ways). Found with a
  watcher copying the file on every change during one `bro format` (12 versions, flipping with each framework's run) and
  a Roslyn probe parsing it with NET20 and NET8_0 symbols. The default engine flips it the same way (`bro format --once
  --files` on it: the net20/net35 runs moved the three lines); its full runs happened to converge here (2 of 2, and in
  PR #291's runs), not investigated why. Fix: `IfBodies.Lines` also protects such a region line (active in one run's
  parse, inactive in another's, below a directive line); `Indentation kept:` names it, its whitespace findings don't count.
  The files were already candidates (`OpensBlock`). After: both engines one changing run then clean, twice each, identical
  trees (the pre-fix run 1's), and only DynamicTests.cs joined Newtonsoft's `Indentation kept:` list. Serilog (init) and
  Polly (migrate): the same trees and output before and after (122 and 2 files, then clean).
  Why CI missed it: `Invoke-RealWorld.ps1` runs `dotnet format analyzers <sln> --diagnostics <BRO ids>` with every
  framework in one workspace: no whitespace pass, no per-framework runs, no IDE rules, so this class can't show there.
  `BroFormat = $true` (Newtonsoft only, default engine (the host since 2026-10-25); owner's choice) adds `bro init --write` + `bro format` on the
  untouched code after the checks; exit 2 fails the job.
  Its first CI run (Windows, newest SDK 10) failed: `Serialization/TraceJsonWriter.cs` flipped `// base.WriteValue(value)
  will error` 8 columns deeper and back. The line sits in the last else body of an if/else chain split by `#if
  HAVE_BIG_INTEGER`/`HAVE_INT128`/`HAVE_HALF`; `IfBodies.Lines` protected the statements there (by their first token) but
  not the comment line, which the formatter indents like the statement below it. Converged locally (SDK 10.0.113, LF and
  CRLF checkouts: 312 files, then clean), so the job now prints the diff of one more run when format isn't clean
  (that's how the line was found). Fix: the `//` lines right above a protected line are protected too.

- **The host is the default engine** (2026-10-25, owner's decision after three clean soaks, trials 52-54;
  docs/decisions/2026-10-25-the-format-host-becomes-the-default-engine.md). `FormatCommand.TakeEngine` and `CurrentEngine`
  give `host` when no `--engine` is named; `--engine dotnet-format` (the old value, kept) is the fallback for at least one
  release. `FormatHost.UsesHost` decides the roll-forward to the newest runtime: `--engine host`, or a `format`/`check`/preview
  command line (after `Program.Expand`) that names no engine; not for init/migrate/baseline without `--diff`.
  Where the default path still runs `dotnet format` processes (each prints why, or is per run and silent):
  - the host can't run (`Engine: the host can't run here ...`, once per command): no SDK folder for `dotnet --version`, an
    SDK without `DotnetTools/dotnet-format/dotnet-format.dll`, SDK 8's dotnet-format (MSBuildLocator: a global.json that
    selects SDK 8, trial 49's Cocona), no runtime as new as the SDK's dotnet-format needs, StyleBro.FormatHost.dll missing,
    any exception while opening it;
  - the workspace doesn't load (`Engine: the host couldn't load the workspace ...`, then the rest of the command): trial 50's
    AdditionalFiles/compiled file;
  - a run with an option `FormatArguments.Parse` doesn't take (`--folder`, `--binarylog`, an unknown pass-through option, a
    `--severity`/`--verbosity` value it doesn't know): that run only;
  - the IDE-suppression probe (`FormatHonorsIdeSuppressions`, one `dotnet format style` per process) and the builds
    (`check --build`, generators): not format runs, the same on both engines.
  Covered by the host as by the old engine (same code before the run decides): `--files` (RunFiles -> Repeat/Verify ->
  the same per-run host scope), Visual Basic (left alone: `--exclude **/*.vb`, as before; a project that is neither C# nor VB, an
  F# one, gets dotnet format's own "only C# and Visual Basic" error from the host too), workloads (a framework that needs a missing workload is left out before the runs),
  configurations (one workspace per configuration), `--report`, `--once`, `--all`, the kept-findings copy of `bro check`
  (the repository's workspaces; a copy missing a file gets its own host, not `dotnet format`).
  Parity before the switch (Compare-Format, host side naming no engine, 4 cores, SDK 10.0.113; times in format-engine.md):
  same trees, exit codes and --json on all six; output the same on four, and on Newtonsoft.Json and Serilog only the
  load-warning note names another run (the old engine's forced-framework "project reference without a matching metadata
  reference", Serilog's in net10.0). Setup notes: lower every global.json (Newtonsoft's Src/, PalCalc's adonis-ui/ pins
  5.0.400), `EnableWindowsTargeting` for PalCalc, Newtonsoft's package reference in Src/Directory.Build.props (its projects
  don't import the root). Seen on Serilog, not changed here: the host's note says to add `--verbosity diagnostic`, but at
  that level it lists no warning (a follow-up).

- **Engine report parity** (2026-10-25, backlog 076, trial 55's soak). Both reproduced on the trial commits (protobuf-net.Grpc
  3471179, ZLinq f7591df) with a debug build logging each run's view, the instance behind each finding and each run's files.
  - protobuf-net.Grpc, 28 more findings with the host (23 IDE0055 beside WHITESPACE, 5 BRO in ContractOperation.cs): not
    re-analysis (both engines' checks analyze the solution the whitespace formatter returned). The net10.0 run's view had
    protobuf-net.Grpc twice: Test.Integration(net10.0) references ClientFactory, which has no net10.0, so its net8.0
    instance came in with its own reference to protobuf-net.Grpc(net8.0). dotnet-format's whitespace formatter formats one
    copy per file path, so the analyzers reported the other instance's unformatted copy. `Views.Of` retargeted only the run's
    own instances' references (trial 47); now every reference in the view to a project the run names goes to the named
    instance, as a `dotnet format` run of that framework loads one instance per project. After: the host's check lists the
    same 1,072 finding lines as the old engine.
  - ZLinq, 71/220/220 vs 69/218/218 "files with findings": the old engine's net6.0 part 2 run loads ZLinq (no net6.0) with
    all its frameworks and so checks and fixes `Internal/SingleSpan.cs` and `Internal/Polyfill/ArraySegmentExtensions.cs`
    (netstandard2.0-only BRO1519); net8.0 and net9.0 then check those files again because an earlier run changed them (the
    `Touched` files), their own checks found nothing there. The host loads the instance the build uses. Owner: a documented
    difference of the fallback engine (docs/decisions/2026-10-25-the-old-engines-loose-load-of-a-referenced-project-stays-a.md).

- **Tracker bugs** (2026-10-04, docs/beyond-stylecop.md "Possible bugs to check", all fixed): BRO1504 treats a
  collection expression's `[` like `{` (StyleCop #3766); BRO1603 accepts a period before closing `)`/`]`/`"`/`'`
  (#2860), puts the period after a trailing entity (`&gt;.`), and skips `stylebro_exclude_from_punctuation_check` tags
  (StyleCop's excludeFromPunctuationCheck, default `seealso`; never checked anyway); BRO1606 judges a summary that
  starts with `<para>` by the paragraph (fix inserts inside it); BRO1510 measures an accessor from its keyword, not
  its attribute line (StyleCop #3434 counts it: documented both ways in parity `accessor-layout`); BRO1505 wants a
  blank line below a multi-line field, exactly SA1516's measure (from the end of the last attribute list's trivia),
  and BRO1001's sort (`NeedsSeparation`) and BRO1114's split add it (FixOrderTests). Migrate: non-English
  `documentationCulture` turns BRO1604-BRO1607 off with a note; `excludeFromPunctuationCheck` != `["seealso"]` is
  written.

## xUnit fixes in format (backlog 073/074, 2026-10-18)

- Owner's choices (docs/decisions/2026-10-18-which-xunit-fixes-bro-format-applies.md): `bro format` adds an allowlist of
  xunit.analyzers ids (`BaselineSuppressor.XunitIds`: xUnit1014/1025/1048 and the assertion rewrites) to `--diagnostics`
  where a project/props/targets/packages.config references xunit, xunit.v3(.mtp-*) or xunit.analyzers
  (`FormatCommand.ReferencesXunit`); `--severity warn` leaves the repo's severities in charge; the baseline covers them;
  init writes nothing; v2 and v3 alike. Docs: docs/xunit.md. Left out because they changed behaviour in the survey:
  xUnit1013 (`[Theory]` on CacheCow's `ILogger.Log`), xUnit1030 (a stateless test needs to lose its sync context).
- Survey: maintained repositories have no xUnit warnings left (the build shows them); the assertion fixes cascade
  (2004 -> 2009/2017, 2010 -> 2000 -> 2013) and converge within `MaxRuns` to one text in every order. Measuring newer
  analyzers than a repo pins: a `CustomAfterMicrosoftCommonTargets` hook that swaps the `Analyzer` items after
  `ResolveLockFileAnalyzers` (trial073 logs). A symlinked folder (two paths to one file) crashed `bro format` (duplicate
  key): use a path fix for case-sensitivity workarounds instead.
- Coexistence fixes: `[MemberData(nameof(x))]` (xUnit1014 writes it) no longer keeps x's rename (`CamelCaseNames.IsMemberDataName`,
  also in RepositoryNames); BRO1602 drops the empty `//` lines BRO1120 would remove next run (`WithoutEmptyEnds`).
