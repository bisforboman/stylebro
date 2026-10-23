# Migration tool, baseline, init and preset packaging

How `bro migrate` reads and maps a StyleCop/StyleSharp setup, baseline support, the preset packaging bug that led to `bro init`, and the first runs through the package. Moved from CLAUDE.md on 2026-10-23.

## Migration tool (`bro`, 2026-10-01)

- User's decision: porting = "generate config from theirs". The preset stays the default for new projects; the tool
  reads a repo's StyleCop setup and writes matching settings, so a StyleCop-clean repo stays (almost) unchanged.
  Usage and rules: `docs/migrating.md`. Code: `StyleCopSetup.cs` (reading), `Migration.cs` (mapping, plan, render),
  `Suppressions.cs`, `Program.cs` (report). Tests: `MigrationTests.cs`.
- Reading: defaults (inventory) <- rulesets <- globalconfigs (strictest wins within a kind) <- root .editorconfig
  sections for all C#. Sub-directory .editorconfigs and path sections are *scopes*: each gets, in its own file and
  section, only the generated lines that differ from the main block.
- BRO rules: on only when ALL replaced SA rules are on (weakest severity); stylecop.json can make some moot
  (elementOrder without constant/static/readonly -> SA1203/SA1204/SA1214; `_camelCase` -> SA1309). The first version
  used the strongest, which turned on BRO1001's full sort in Polly (SA1201/SA1202 off) and changed 244 files.
- SDK rules: strongest severity of the covered SA rules, options from stylecop.json; keys the root .editorconfig
  already sets for C# are left out (OpenTelemetry sets `csharp_preserve_single_line_statements = true`; overriding
  it split `case X: a(); break;` lines).
- Suppressions carried over with `--write`: `#pragma warning disable SA...` (+ BRO/IDE ids), `[SuppressMessage]`
  (a sibling attribute per replacing id, own attribute list so BRO1102 stays quiet), `<NoWarn>` in
  .csproj/.props/.targets (Polly's Snippets project has `SA1123` in NoWarn: its `#region`s are doc snippets, and
  BRO1113 removed 147 of them before). The StyleCop ids stay. Idempotent.
- `--write` turns the preset off (`<StyleBroPreset>none</StyleBroPreset>` in the root Directory.Build.props) and the
  block writes every preset key (test `TheGeneratedSettings_CoverEveryPresetKey`). Reason, probed: `dotnet format`
  sorts usings whenever `dotnet_sort_system_directives_first` is SET (any value, even `false`; `unset` doesn't help; in
  the style pass, IDE0055's severity doesn't matter; so since 2026-10-11 the PRESET doesn't set it and `init` writes it
  only when the code sorts its usings, docs/decisions.md),
  and an .editorconfig can't remove a key the preset's global config sets. The private app has SA1208/SA1210 off and
  unsorted usings: the block writing `true` changed ~95 files, `false` ~1,650. Now the sort keys are written only
  when SA1208 or SA1210 is on.
- **StyleSharp.Analyzers** (SST ids, a StyleCop successor; owner 2026-10-14 "Read it like StyleCop"): a reference counts as
  StyleCop running; SST severities (its defaults + configs, raw in `StyleCopSetup.Raw`) are translated to SA/SX ids
  (`StyleCopSetup.Translate`, mapping `scripts/stylecop-survey/data/stylesharp-mapping.csv` from New-StyleSharpMapping.ps1
  over `inventory-stylesharp-5.1.2.csv`; exceptions: SST1117 -> SA1101/SX1101 by `stylesharp.instance_member_qualification`
  (default omit_this), SST1309 -> SX1309/SX1309S (BRO1303 `_camelCase`, SST1306 ignored), SST1150-1171 -> SA1117, SST1215 ->
  SA1214, SST1633 only with file_header_template, SST1116/SST1119 nothing); `stylesharp.*` keys -> synthetic stylecop.json
  (`StyleSharpUse.Settings`), stylecop.json NOT read (StyleSharp doesn't); SST suppressions carried over
  (`Suppressions.WithStyleSharp`); the block records `# StyleSharp.Analyzers version:` for after the swap. Both packages:
  a rule is on when either enforces it. Leftover StyleCop settings (no package) -> migrate points to init, Sonar or not.
  **Match StyleSharp's behaviour** (owner 2026-10-16, trial 24 Refit: SST1624 build errors after BRO1604's fix): for
  StyleSharp alone (`Migration.StyleSharpOnly`) migrate writes `stylebro_summary_wording = stylesharp` (BRO1604-BRO1607),
  `stylebro_member_order` with `conversion|operator|extension|method` (`|` = shared rank) and `record` (records rank with
  classes unless listed), `stylebro_member_order_namespaces = false`, `stylebro_trailing_comma_switch_expressions`/
  `_with_expressions = false` (also StyleCop 1.1.x), empty blocks and `};` allowed, SST1305's word list and SST1629's
  exclusions via `StyleSharpUse.Settings`, BRO1312 off (`StyleSharpLeavesAlone`). Probe: `Compare-WithStyleCop.ps1
  -StyleSharp` (new SST findings after BRO's fix = build breaks; BRO findings on StyleSharp-fixed code = churn); the
  remaining churn is listed in docs/migrating.md.
- SX ids are read too (the reader used to accept only SA ids, so SX settings were silently ignored): SX1101 -> IDE0003,
  SX1309 -> `_camelCase`; SA1412 on -> `charset = utf-8-bom`.
- Also read: the StyleCop.Analyzers version (1.1.x: SA1141/SA1142/SA1316/SA1414 off) and whether any project sets
  GenerateDocumentationFile. Without it StyleCop's XML doc rules never run in the build (SA0001), but `dotnet format`
  parses docs anyway, so BRO1603-BRO1611 stay off (the private app: 5 `<placeholder>` unwraps before).
- Report: rules on but not enforced afterwards, grouped: partly covered / not expressible (with the reason),
  dropped by design (from mapping.csv), not covered yet (rule titles).
- Found on the way, fixed in the preset too: `csharp_preserve_single_line_blocks = false` expands every
  `{ get; set; }` (StyleCop allows them), so it's `true` and SA1501/SA1502 aren't SDK-covered (mapping: SDK 49 -> 47);
  IDE0040 `always` adds `public` to interface members, SA1400 doesn't ask for that: `for_non_interface_members`.
  Also: StyleCop has SA1124 ON by default (both DLL inventories); BRO1112 stays off in the preset (opt-in, large
  one-time change), the docs no longer claim it matches StyleCop.
- Measuring (`scripts/stylecop-survey/Measure-MigrationDelta.ps1`, needs `-ResetTargetRepo` on a throwaway clone): plain `dotnet format` committed as baseline (it has its own noise:
  conflict markers in multi-targeted files, line endings), then migrate --write, then `dotnet format` with StyleBro
  and the preset; the remaining diff is StyleBro's. The hook uses COPIES of the DLLs (in %TEMP%), otherwise the run
  locks the build output, and it drops StyleCop.Analyzers' analyzers (a real migration removes the package; its SA1651
  fix ran under `dotnet format`, which parses docs even where the build doesn't).
- Results (2026-10-01), files StyleBro changes on StyleCop-clean repos after migrating: Polly 244 -> 3 (2 IDE0047 lines
  removing parentheses in `a ?? (b ?? c)`, which SA1119 accepts; 1 file plain `dotnet format` had already broken with
  conflict markers), OpenTelemetry 22 -> 1 (a line ending in a mixed-EOL file), private app 1,659 (mid-way) -> 10
  (9 IDE2000 blank lines at a file's start / before `}`, which SA1507 leaves to SA1517/SA1508, both off there; 1
  BRO1104 on target-typed `new()`, which StyleCop 1.1.118 misses). The IDE0047/IDE2000 differences are documented
  in docs/migrating.md; the SDK rules can't be narrowed.
- StyleBro bugs found this way (fixed): BRO1606 replaced "Initializes ... class representing X." (text lost); BRO1606
  flagged `{TResult, TArgs}` crefs; BRO1601 ignored documentInternalElements; BRO1611 checked constructors/operators
  (StyleCop doesn't); BRO1113 took regions in expression bodies (StyleCop: SA1124, so BRO1112).

## Baseline (2026-10-02)

- User's request: "baseline support" (fail only on new violations). Docs: `docs/baseline.md`.
- Probed first: `dotnet format` honors a `DiagnosticSuppressor` (suppressed BRO and IDE diagnostics aren't fixed), and
  `dotnet format --verify-no-changes --report dir` lists every analyzer/style diagnostic it would fix (suppressed ones
  left out). Whitespace formatting is NOT a diagnostic there ("WHITESPACE"), so it can't be baselined (documented).
- `src/StyleBro.Analyzers/Baseline/`: `Baseline` (file format: tab-separated rule, path relative to the baseline,
  fingerprint = 64-bit FNV-1a of the trimmed line text, count; sorted render) and `BaselineSuppressor` (every BRO id
  via reflection over `DiagnosticIds` + `SdkIds`, the IDE rules the preset/migrate can turn on; a MigrationTests guard
  keeps `SdkIds` in sync). Each entry covers `count` diagnostics of its rule on a line with that text, in source order,
  per compilation (multi-targeting: each TFM on its own). Moved lines stay hidden; an edited line's violations are new.
- Package targets: the nearest `stylebro.baseline` above the project (`GetPathOfFileAbove`) becomes an AdditionalFile;
  `<StyleBroBaseline>` overrides the path (file name must stay `stylebro.baseline`), `none` turns it off. Checked by
  packing a test version and consuming it.
- `bro baseline [path] [--project x]` (`BaselineCommand.cs`): moves an existing baseline aside, runs
  `dotnet format --verify-no-changes --severity warn --report`, dedupes TFM copies, writes the file, prints counts per
  rule, whitespace changes and uncovered ids (other analyzers).
- FFMpegCore end to end (all rules + preset, TreatWarningsAsErrors): 820 violations on 759 lines baselined; then build
  0 StyleBro errors (519 without), `dotnet format --verify-no-changes` exit 0; a new file's violation and an edited
  baselined line were reported and fixed.
- Found on the way: **ListGapsAnalyzer crashed (AD0001)** in FFMpegCore's tests: BRO1117 compared the previous item's
  START line with the comma's, so a multi-line raw/verbatim string ending right before the comma looked like a comma on
  a new line and `LastIndexOf('\n')` returned -1. Now compares the token's END. Earlier real-world runs missed it
  because realworld.ps1 only reported NEW compile errors; it now prints analyzer crashes separately. AD0001 sweep of all
  six repos with every rule: see the log.

## Preset packaging bug and `bro init` (2026-10-02)

- Found while writing the CI guide: **the preset never reached the compiler through the NuGet package (alpha.1 to
  alpha.8).** The package's build targets added it as `GlobalAnalyzerConfigFiles`, but the SDK turns those into
  `EditorConfigFiles` in an ItemGroup of Microsoft.Managed.Core.targets, evaluated BEFORE a package's build targets
  (only the SDK's analysislevel globalconfig showed up in `/analyzerconfig`). Every real-world run used a hook adding
  `EditorConfigFiles`, which hid it. Effect for package users: BRO rules at their default severity (BRO1112 regions ON
  instead of off), no preset options. Fix: the targets add `EditorConfigFiles` (the file has `is_global = true`).
- Probed then: `dotnet format`'s style pass IGNORES rule severities from global configs (any `global_level`, also a
  repo `.globalconfig`; `--diagnostics IDE0040` doesn't help), reads them from `.editorconfig`; the build honors
  both. Formatting OPTIONS from a global config are honored by `dotnet format` (`csharp_preserve_single_line_statements
  = false` split a line), and severity `none` for BRO rules too. User's decision: the preset keeps BRO severities and
  the SDK options; the 12 `dotnet_diagnostic.IDE*` severities moved to `src/StyleBro.Migrate/sdk-rules.editorconfig`,
  which `bro init [--write]` (`InitCommand.cs`) writes into the root .editorconfig between the
  stylebro-migrate markers (a `--write` block wins; it has them too). Tests: `ThePreset_SetsNoBuiltInRuleSeverities`,
  `Init_WritesTheBuiltInRulesOnce_AndLeavesMigratedSettingsAlone`. Checked through a locally packed package: build
  gets IDE0040/IDE0036/IDE0055, and after `init --write`, `dotnet format` fixes them.
- When the self-check moves to a release with this fix (`StyleBroSelfVersion`), the preset starts applying to
  StyleBro's own code too (e.g. using sorting): run `dotnet format` and commit that separately.

## First run through the package (2026-10-03)

- `scratchpad/pkgrun.ps1`: the package from a local feed (hook adds only a PackageReference + feed, isolated
  NUGET_PACKAGES), `bro init --write`, then plain `dotnet format` (whitespace + style + analyzers), i.e.
  exactly a new user's first run. FFMpegCore: 142 files in one pass, compiles, second run clean.
- **Serilog: `dotnet format` wrote NOTHING.** First "Changes must be within bounds of SourceText" in BRO1108's Fix All:
  `LinkedFileFixAllProvider` (and `CamelCaseRenamer`) applied every linked copy's edits to the FIRST copy's text, but
  `dotnet format` runs its whitespace and style phases first and can leave a multi-targeted file's copies DIFFERENT.
  First fix: copies with identical text share edits, diverged copies fixed on their own. That turned Newtonsoft.Json
  into 1,722 conflict-marker lines (StyleBro alone, no `init`): whole-file fixes (BRO1001) made different edits in
  each copy, which `dotnet format`'s merge can only write as conflicts. Final: when a file's copies differ, ONLY THE
  FIRST copy (group of identical copies) is fixed and the rest left unchanged; Roslyn's merge combines its edits with
  the others' untouched text, and `#if`-only fixes come on the next run. Renamer: one text variant per path (file key
  = path|checksum, first key per path wins). Test `CopiesWithDifferentText_OnlyTheFirstIsFixed`.
- Then still nothing written: the SDK's own fixers break multi-targeted repos under `dotnet format` once `init`'s
  severities are on, WITHOUT StyleBro too (reported upstream: https://github.com/dotnet/roslyn/issues/86000 and https://github.com/dotnet/roslyn/issues/86001) (plain `dotnet format` alone fine). Serilog: IDE0011 crashes Roslyn's
  `LinkedFileDiffMergingSession` (unhandled exception, nothing written), IDE0040 writes conflict markers;
  Newtonsoft.Json: plain dotnet format + init severities = 172 conflict-marker lines (79 files, 0 markers without).
  Bisect script `scratchpad/bisect-ide.ps1` (one rule at a time): IDE0011 crash, IDE0040/IDE0047 markers, IDE0055 at
  warning crash (its fix runs in the style pass), IDE0048 a broken net462 edit; with StyleBro also IDE2000-2003 (one
  blank-line conflict per repo). `init` detects multi-targeted projects (`<TargetFrameworks>` with ';' in a .csproj or
  .props) and writes those 8 as `suggestion` (`InitCommand.UnsafeWhenMultiTargeted`); `--write` (StyleCop) keeps them
  and adds a report note. Final first runs (package + init): Serilog 169 files, Newtonsoft.Json 704 files, 0 conflict
  markers, 0 new compile errors; Newtonsoft needs 2 runs (2nd: 135 files), then only the 49 guard-kept renames remain
  (the report lists each once per target framework: 270 entries). FFMpegCore 1 run.
- DocumentationAnalyzer prefilter (skip members that can't be overrides/implementations) measured NO gain (143-150 vs
  155-166 ms): reverted.
