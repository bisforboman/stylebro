# StyleBro

Roslyn analyzers + code fixes for C#: a modern replacement for StyleCop.Analyzers
(last stable release 1.1.118, stuck in 1.2.0-beta for years). Name: **StyleBro** (the friendly successor to StyleCop: it fixes things for you instead of writing tickets). Rule prefix `BRO`, config keys `stylebro_*`.
The project was renamed from "Kempt" (pure text/file-name replacement). Repo: `github.com/bisforboman/stylebro`.


## Core idea

Only analyzers **with code fixes that `dotnet format` can apply**. The goal is that running
`dotnet format` fixes everything StyleBro reports. Formatting-only rules are out of scope where the SDK
already covers them (IDE rules, `.editorconfig`); the preset enables those built-in rules instead.

## Status

**Stable 1.0 (owner's decisions, 2026-10-11, docs/decisions/):** 1.0 ships after two clean trials in a row (backlog
item "Two clean trials before 1.0" keeps the tally; each trial's verdict is in docs/notes/trials.md); after 1.0, new rules
stay off until a preset version opts in. What patch/minor/major may change: `docs/versioning.md`; releases themselves
still follow the release policy below (the owner says when). CHANGELOG.md is the full release history (each version's
section is its GitHub release notes; the newest `## X.Y.Z` there is the latest release), SECURITY.md points to private
vulnerability reporting. Latest on nuget.org: 0.8.0-alpha.1 (2026-10-19), and `StyleBroSelfVersion` is 0.8.0-alpha.1.
From alpha.5 on, release.yml also packs `bro` (package StyleBro.Tool, a .NET tool) with the same version; CI packs both
too. Older release notes and the earlier status history: docs/notes/history.md.

**Release policy (user's decision, 2026-09-30):** don't publish a version for every batch of rules while the project
is starting out. Keep adding rules on `main` and release again once there's a critical mass of new rules. Don't
suggest or push a release tag after each batch. Since 2026-10-03 (user: "don't focus on releases yet, I'll let you know
when we can do that"): don't suggest releases at all; the user says when.

## Layout

- `src/StyleBro.Analyzers`: netstandard2.0, references Microsoft.CodeAnalysis.CSharp only.
  Must never reference Workspaces (RS1038).
- `src/StyleBro.CodeFixes`: netstandard2.0, Workspaces, code fixes + Fix All providers. References Analyzers.
- `src/StyleBro.Package`: packs both DLLs into `analyzers/dotnet/cs`, plus `build/StyleBro.Analyzers.targets`
  that adds the preset layers `build/presets/stylebro.X.Y.globalconfig` (global_level -1) up to `<StyleBroPreset>`.
  **Preset versions (2026-10-13, docs/versioning.md, docs/proposals/preset-versions.md):** one layer per minor holding
  only that minor's new rules and keys (no key in two layers: same-level global configs conflict); values `X.Y`,
  `latest`, `none`; unset = the package's major (`_StyleBroPresetDefault`, 1.0); newer than the package = STYLEBRO002
  warning, anything else STYLEBRO001 error; added at evaluation time with `VersionGreaterThanOrEquals`. Rules added
  after 1.0 MUST be `isEnabledByDefault: false` (only the preset turns them on, so `none` and migrated repos stay quiet;
  probed: a global-config `severity = warning` enables such a rule in the build and in `dotnet format`). The version a
  rule was added in = its AnalyzerReleases.Shipped.md header (pre-1.0 = 1.0; Unshipped = `PresetVersions.Latest`).
  A minor's first new rule: add `presets/stylebro.1.1.globalconfig`, its line in the targets, raise
  `_StyleBroPresetLatest` and `PresetVersions.Latest` (PresetVersionTests checks all of it). `init --write` pins
  `PresetVersions.Latest` in Directory.Build.props (`Migration.SetPreset`); `--write` keeps `none`. Tests count a rule the
  latest preset turns on as on (Verifier, DocExamplesTests); realworld enables newer layers' rules; StyleBro's own code
  uses `latest`.
  **Styles** (docs/styles.md): `<StyleBroStyle>microsoft</StyleBroStyle>` adds `presets/styles/microsoft.X.Y.globalconfig`
  on top of the preset (`init --style microsoft`); `stylecop` (2026-10-18, owner) only names the default, no layer.
- `src/StyleBro.Migrate`: net8.0 console tool `bro` (package **StyleBro.Tool** since 2026-10-18, owner's decision; was
  `stylebro-migrate` in StyleBro.Migrate; the project and assembly keep the old name; `bro check` = `format
  --verify-no-changes`, `bro preview <cmd>` = `--diff`, `bro migrate` = the bare path form, `Program.Expand`; markers
  in users' files keep `stylebro-migrate`) (PackAsTool, published from alpha.5 with the analyzers;
  its package README is `src/StyleBro.Migrate/README.md`), see docs/notes/migration.md.
  References StyleBro.Analyzers (reads every rule's "Replaces StyleCop SAxxxx" from its description) and embeds
  `scripts/stylecop-survey/data/inventory-1.2.0-beta.556.csv` and `data/mapping.csv` (written by New-Mapping.ps1).
- `tests/StyleBro.Tests`: xunit + Microsoft.CodeAnalysis.CSharp.CodeFix.Testing 1.1.2 (`DefaultVerifier`).
- `samples/Messy`: dotnet format integration sample. `Input/*.cs` is copied to `Generated/` (gitignored),
  formatted, compared with `Expected/`, then a second run with `--verify-no-changes` must pass.
- Central package management in `Directory.Packages.props`. Roslyn pinned to **4.8.0** for broad SDK/VS
  compatibility; don't raise it without a reason.
- `scripts/stylecop-survey`: generates `docs/stylecop-mapping.md` and `docs/skipped-rules.md` (inventory of StyleCop rules, repo surveys,
  SDK coverage check). Hand-written decisions in `decisions.psd1`, measured data in `data/`. Not part of the slnx.
  **Every StyleCop rule StyleBro doesn't cover has an entry in `skipped.psd1`** (status Candidate / SdkLater / Drop /
  NotInStyleCop / Variant / NotApplicable, why, what revisiting would take), rendered as `docs/skipped-rules.md`
  (user's request, 2026-10-01: "so they can easily be revisited later"). When a rule gets implemented, move it to
  `decisions.psd1`; when a new reason to skip one turns up, write it there. New-Mapping warns about missing entries.
- `samples/MultiTarget`: net10.0 + net8.0 with `#if NET10_0_OR_GREATER` code; verify-format checks it like Messy and
  also that the fixed sample builds. It does NOT reproduce the old linked-file bug (couldn't find a minimal repro);
  the reference for that bug is Newtonsoft.Json (8 target frameworks) with all rules in the real-world run.
- **Docs site** (2026-10-09, docs/decisions.md): MkDocs Material, `mkdocs.yml` at the root (docs_dir docs, explicit
  `nav`, `exclude_docs` for the internal decisions/backlog/beyond-stylecop/proposals), pinned packages in
  `.github/docs-requirements.txt`. `.github/workflows/docs.yml`: `mkdocs build --strict` on every PR (broken links/
  anchors, pages missing from the nav, and links to excluded pages fail), deploy to GitHub Pages on `main`
  (https://bisforboman.github.io/stylebro/). The deploy job fails until the owner sets Settings > Pages > Source:
  GitHub Actions; it isn't a required check. Help links (`Descriptors.HelpBase`) point to the site's
  `rules/BROxxxx/`. A new page needs a nav entry (rule pages under their area, in index order: `DocExamplesTests`);
  links leaving docs/ or to excluded pages are absolute GitHub URLs. `docs/configuration.md` lists every `stylebro_*`
  key read in src/ (test). Preview: CONTRIBUTING.md.
- `scripts/verify-format.ps1` reads the rule IDs from `AnalyzerReleases.Unshipped.md` and runs every sample.
- `scripts/verify-package.ps1` (CI and release.yml, 2026-10-02): packs StyleBro.Analyzers as 0.0.0-verify into a temp
  feed, restores it (isolated RestorePackagesPath) into a project OUTSIDE the repo and checks what only the package's
  build targets do: preset reaches the compiler (BRO1112 off, BRO1106 on), `StyleBroPreset=none`, `init --write` +
  `dotnet format` (IDE0040/IDE0036/BRO1106 fixed, preset option applied, second run clean), baseline. Verified that it
  fails (3 checks) with the old GlobalAnalyzerConfigFiles targets.
- **StyleBro checks its own code** (2026-10-02) with its latest PUBLISHED release (`StyleBroSelfVersion` in
  Directory.Build.props, bump it after each release): a PackageReference in Migrate and Tests (the real install path);
  StyleBro.Analyzers/CodeFixes can't (StyleBro.Package references them and is the package itself: NU1108 restore
  cycle), so they `PackageDownload` it and Directory.Build.targets adds its DLLs as Analyzers and imports its build
  targets. Samples and StyleBro.Package are left out. First run: ~535 findings (190 BRO1601 inheritdoc, 144+144
  BRO1107/BRO1108 on `VerifyFixAsync("""...""", """...""")` in tests, BRO1303 unified `_field`/`field` to `field`), 142
  files changed, builds, 397 tests pass, second run clean. CI runs
  `dotnet format StyleBro.slnx --verify-no-changes --severity warn --exclude samples`.
- Before pushing, run the CI self-check locally: `dotnet format StyleBro.slnx --verify-no-changes --severity warn
  --exclude samples` (the push of 2026-10-03 failed CI on doc summaries and blank lines in new code).
- **GOTCHA (found 2026-10-03): two copies of every fixer.** `dotnet format` on a sample also loads the referenced
  StyleBro.Analyzers/CodeFixes projects, which carry the PUBLISHED StyleBro (self-check) as analyzers; with two fixers
  for one rule it picks either, so verify-format sometimes tested the release, not the code (BRO1519's blank lines came
  and went: 1 run in 3-4). verify-format.ps1 now sets `StyleBroSelf=none` (an explicit value wins in
  Directory.Build.props). The CI self-check (`dotnet format StyleBro.slnx`) can likewise pick the dev fixers through
  Messy's project references; harmless while they agree.
- C# files under `src/` and `tests/` use LF line endings (normalized 2026-09-30; a few had become mixed from scripted
  edits inserting CRLF, which then made exact-text replacements fail). Keep new edits LF.
- `.github/workflows/ci.yml`: ubuntu-26.04, .NET 10; runs test, verify-format, pack, uploads the nupkg; a `parity` job
  runs every StyleCop parity set (`Compare-WithStyleCop.ps1`).
- **Advisor**: when an advisor tool is available, call it before design decisions or recommendations, before splitting
  work across agents or sessions, and before declaring a larger task done.
- **Asking the owner** (2026-10-17, owner: "Remember to prompt me when you want any input from me"): whenever work needs
  the owner's input (a design choice, a Windows-only step, an approval), ask with the AskUserQuestion tool, which
  prompts them, never only in prose, a PR body or a log; then carry on with what doesn't depend on the answer.
- **Where work runs**: long work (multi-step tasks, PRs, CI waits, releases) runs as cloud sessions; the owner's
  computer must stay free to restart or turn off. Work that needs it (local timings, Windows-only checks) is asked for,
  never started unasked; a cloud session that needs such a step says so in its PR.
- **PR pipeline (2026-10-03, user's request: "verify new changes before merging to main")**: `main` is protected; changes
  land through pull requests whose checks pass. Work on a branch, push it, open a PR (`gh pr create`), let the checks
  run, merge when green. Checks: `build` + `parity` (ci.yml), `Real world (<repo>)` (realworld.yml: one
  job per public repo in `scripts/realworld/repos.psd1`, on the runner its `Os` names (windows-latest by default: .NET
  Framework targets and CRLF checkouts; Jellyfin, FluentValidation, eShopOnWeb, Mapperly on ubuntu-26.04 since
  2026-10-10, same results as on Windows; Serilog/MediatR lose their net462 findings on Linux), prints each phase's
  duration; the untouched code's failing tests are cached (key: repo, commit, SDK, OS, `FailedTests.ps1`, the repo's
  Tests/TestFilter; saved outside PRs after a successful run), so a hit skips that Release build and test run; `scripts/realworld/Invoke-RealWorld.ps1 -Tests`: every rule,
  fails on analyzer crashes, new compile errors, conflict markers, a second run that still changes files (since
  2026-10-04; `MaxRuns` in repos.psd1 for a documented exception), or tests that passed
  on the untouched code and fail after the fixes; OpenTelemetry without tests), `mutation` (mutation.yml:
  `scripts/mutation/Invoke-Mutations.ps1` breaks each guard listed in `scripts/mutation/entries/<Area>/<file>-<hash>.psd1` (one file per entry, so parallel PRs
  don't conflict; write it with `scripts/mutation/New-Mutation.ps1 -File -Find -Replace -Tests [-Comment]`, which picks the
  folder and the name) and needs a test to fail; add an entry for every new guard; entries are sorted by path; parallel shards `mutation shard N` (`-Shard i -Shards n`, index modulo n: 4 on PRs, 8 for
  the full nightly run since 2026-10-18, which outgrew 4 x 60 minutes; timeout 90), each shard also runs the stale check over all entries), and the job `mutation` passes only if every shard did).
  **Scoped PR runs (2026-10-09, owner's decision)**: each workflow's `changes` job (`.github/changes.sh`, git diff
  against the base) decides; a docs-only PR (docs/, `*.md` outside src/tests/samples, New-Backlog.py, survey files
  parity/Migrate don't read) skips the work of parity, real world (on ubuntu) and mutation, but every required check
  still reports (step-level `if`s: a job-level `if` on a matrix job hides its names); performance only runs when
  src/, build settings or the benchmark change. On PRs mutation runs only entries whose `File` (or test class file)
  changed (`-OnlyFiles`); a guard broken through another file (shared helper) is caught by the full run on main. `build`
  always runs (docs are tested). All scripts run locally too (mutations in a separate worktree: they undo
  each mutation with `git checkout`, so commit first).
- **Fewer conflicts** (2026-10-18, owner's choices, after measuring PRs #173-#252: 3 real conflicts in 31 main merges,
  all between sibling PRs of one trial; 13 of 15 recent merges unnecessary, 5 follow-up commits only repaired stale
  mutation entries): a trial opens ONE PR (if it must split, a fixed order: the PR touching the shared files merges
  first, the others merge main once after it); merge main into a PR only when GitHub shows a real conflict (DIRTY
  several times) or CI needs main's code, and after any merge run the mutation stale check (every entry's `Find` still
  in its `File`) before pushing; a changes/ fragment is `changes/<branch>.md` with the branch's `/` as a folder.
  docs/getting-started.md keeps the path (install, A, B, first run); its reference sections have their own pages
  (init-conventions, kept-findings, vendored-code, multi-targeting, debug-and-release, old-style-projects), each with a
  short pointer left under its old heading so released tools' links still land: write new detail there, not in the path.
- **Changelog** (2026-10-11, owner's decision, ShotDetector's style): a PR with a change users notice adds
  `changes/<branch>.md` (a `/` in the branch is a folder; changes/README.md: `### Breaking changes` / `### New` / `### Changed` / `### Fixed` /
  `### Upgrading` and bullets), never CHANGELOG.md itself, so parallel PRs don't conflict there. CI's build job runs
  `python scripts/changelog.py --test` and `--check` (every fragment parses). Write for the release notes' reader:
  short, concrete, no internal jargon, never private-app details.
- `.github/workflows/release.yml` (release on merge since 2026-10-11, ShotDetector's design): on every push to main the
  `version` job reads the newest `## X.Y.Z – date` (or `X.Y.Z-alpha.N`) in CHANGELOG.md; if `v<version>` isn't tagged
  yet, the `release` job runs the same checks, packs with that version (`-p:Version`, overrides `<Version>` in the
  csproj), pushes to nuget.org via Trusted Publishing (`NuGet/login@v1`, no stored API key) and creates the GitHub
  release and its tag on that commit, with the version's CHANGELOG section as the notes (prerelease if the version has
  a `-`). A hand-pushed `v*` tag still releases that tag (notes from CHANGELOG if it has the section, else GitHub's PR
  list). Most pushes: the version is tagged, nothing else runs.
  **Release PR**: once every PR going into the release has merged, one PR runs
  `python scripts/changelog.py X.Y.Z-alpha.N "129 rules, each fixed by \`dotnet format\`. Docs: <site>"` (folds and
  deletes the fragments, adds "Upgrading from <previous>" with the tool update line), moves Unshipped -> Shipped
  ("Release 0.x.0.1"), bumps the versions in both csproj and the docs/samples, and drops the docs' *Next release*
  markers; merging it releases. Then bump `StyleBroSelfVersion` in its own PR.
  Set up: nuget.org Trusted Publishing policy (repo owner bisforboman, repo stylebro, workflow `release.yml`,
  environment `release`, packages `StyleBro.*`, new packages allowed), repo *variable* `NUGET_USER` = `bisforboman`
  (nuget.org profile name; a variable, not a secret, so logs aren't masked), and the `release` environment
  requires the owner's approval. Since 2026-10-01 (user's decision: "prereleases auto"): versions with a `-` use the
  `prerelease` environment instead, which has no reviewers; its deployment policy accepts tags matching `v*-*`, and
  must also allow the branch `main` for a release on merge (the owner adds it; until then a prerelease merge fails at
  the environment and publishes nothing, and pushing the `vX.Y.Z-alpha.N` tag on the release commit releases it). It
  needs its own nuget.org Trusted Publishing policy (same repo/workflow, environment `prerelease`). The first failure
  (HTTP 401 "No matching trust policy") was simply a missing policy.

## Decision log

Design questions put to the user are recorded one file per decision in `docs/decisions/<yyyy-mm-dd>-<slug>.md` (question,
choices, answer; user's request, 2026-10-03; split from the single `docs/decisions.md` on 2026-10-11 so parallel PRs don't
conflict, that page now only points to the folder). Add one whenever the user decides a design question:
`pwsh scripts/New-Decision.ps1 -Title "..."` writes the skeleton. `docs/backlog.md` (generated by
`python scripts/New-Backlog.py`, NOT committed since 2026-10-11: every rule PR changed it, so parallel PRs conflicted; the
docs workflow publishes it to the `generated` branch after each merge to main,
https://github.com/bisforboman/stylebro/blob/generated/docs/backlog.md) lists every rule StyleBro has and aims to add,
and open gaps (user's request, 2026-10-03: "easier to track progress and goals"); update an item's status when work
starts or lands. Its planned/work/maybe items are one JSON file each in `scripts/backlog/<list>/<NNN>-<slug>.json`, read
in file-name order (a new item takes the next free number). Since StyleCop parity (2026-10-03, user chose all four
directions): its "Work beyond single rules" list (`scripts/backlog/work/`) holds hardening,
the last parity items, adoption polish and a survey of other analyzers, in that order. Open work from it: StyleBro versions of the SDK
rules that break multi-targeted projects (IDE0011, IDE0040, IDE0047, IDE0048, IDE2000/2002/2003; not IDE0055).

## Design rules for every rule

1. Every diagnostic has a code fix, and **Fix All works**. `dotnet format` applies fixes through Fix All.
   Use `LinkedFileFixAllProvider.Create(FixDocumentAsync)` (src/StyleBro.CodeFixes), never plain
   `FixAllProvider.Create`: in multi-targeted projects each target framework has a linked copy of every file, `#if`
   makes the copies need different edits, and the plain provider left the merge to `dotnet format`, which wrote
   conflict markers (and, with a diff-based merge, duplicated members) into Newtonsoft.Json. The linked-file provider
   merges exact edits per physical file (tree rewrites count as one whole-text edit) and gives every copy the same text.
   Also avoid decisions that depend on `#if`-conditional code (e.g. "which item is last"): skip such cases.
2. Fixes are **deterministic and idempotent**: after a fix, the analyzer reports nothing, and a second
   `dotnet format` run changes nothing. Analyzer and fix share one piece of logic so they can't disagree.
3. When a fix could be unsafe (e.g. preprocessor directives between members), skip the case: no diagnostic
   rather than a wrong fix.
4. Configuration goes through `.editorconfig` keys prefixed `stylebro_`, read via `AnalyzerConfigOptionsProvider`
   in both the analyzer and the fix.
5. Every rule gets a `docs/rules/BROxxxx.md`, a row in `docs/rules/README.md` (the rule index; README.md has only a
   summary per area, and New-Backlog.py reads the index) and in `AnalyzerReleases.Unshipped.md` (both in id order), unit tests covering
   the single fix + Fix All, and a case in `samples/Messy`. Rule pages share one structure: intro, `## Example`
   with `### Before`/`### After` csharp blocks, `## Why`, then as needed How the fix works / Configuration /
   Not reported / Compared with StyleCop. Every rule is also listed in `docs/differences-from-stylecop.md` (under
   "Same as StyleCop" if nothing differs), and every preset value that differs from StyleCop's defaults is explained
   there (user's request, 2026-10-01: "so it's obvious later"); `MigrationTests` enforces both. `DocExamplesTests` runs every example (Before compiles and gets the
   diagnostic, one Fix All pass gives exactly After, After compiles clean), so examples can't go stale.

## Cloud sessions (environment)

- .NET SDK 10.0.1xx + 8.0.1xx come from Ubuntu's packages (builds.dotnet.microsoft.com is blocked by the proxy). pwsh:
  `dotnet tool install -g PowerShell` (link it into /usr/local/bin if missing); a `bro` installed as a tool needs
  `ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`.
- The container runs as root: `Migrate_WithALockedFile_WritesNothing` fails there (and on main). SDK 10.0.1xx's
  `dotnet format` style pass loads no DiagnosticSuppressors: `scripts/verify-package.ps1` check 5 fails locally, passes
  in CI on the newest SDK 10.
- Restores through the proxy: `NUGET_CERT_REVOCATION_MODE=offline` (NU3018, also for `dotnet test`); `-p:NuGetAudit=false`
  where the untouched code fails NU190x under TreatWarningsAsErrors. A repo whose nuget.config `<clear/>`s gets the trial
  feed (and a `StyleBro.*` packageSourceMapping) in that file; lower a global.json pinning a newer SDK; no .NET 9 runtime:
  `DOTNET_ROLL_FORWARD=Major`. More per-repo recipes: each trial's Environment bullet in docs/notes/trials.md.
- ~15 GB of memory: run heavy jobs (a large format, the test suite) one at a time (OOM kills show as exit 137 / "Test host
  process crashed"). Worker restarts kill background jobs: check their logs before rerunning; start long reruns with
  `setsid nohup`. `pkill -f`/`pgrep -f` with a pattern from the same command line matches the calling shell.
- Trial clones are third-party repositories: commit there with the noreply identity if needed, never push them (the stop
  hook flags their commits and dirty trees). Delete unused clones in `.trials/`: their `*.verified.txt` files make
  verify-format and snapshot tests fail or spin (SnapshotFiles scans the git root on disk).
- In a bash heredoc python script `\\n` became a real newline: write such scripts with the Write tool.

## Gotchas (short; the history behind each is in docs/notes/)

- Commit before testing mutations: `Invoke-Mutations.ps1` undoes each mutation with `git checkout <file>`, which reverts
  uncommitted work (bitten three times). Mutation entries' `Tests` must not start with `FullyQualifiedName~` (the runner
  adds it); give them a NARROW filter (a FixOrderTests case with 7 ids is 5,040 orders).
- After merging main: build before trusting mutation results (a compile error "kills" every mutation), re-run the tests,
  and check every mutation entry's `Find` still exists in its `File`. merge=union can double or resurrect lines (and give
  two same-named Messy samples): check the merged files. Agents squashing onto a moved main can revert other PRs' changes:
  check a PR's file list.
- Fix order under `dotnet format` is not fixed (one Fix All per id, in an order that changes between processes). A fix
  that changes a node's shape must also produce what other rules want for the new shape; options of different rules
  interact (ask the other rule's CONFIGURED decision). Add a `FixOrderTests` case (it asserts identical TEXT in every
  order) and run verify-format several times when adding a layout rule; compare preview and format output file by file.
  FixOrderTests never sees the whitespace pass. A new case or rule changes `src/StyleBro.FormatHost/FixInteractions.txt`
  (the format host's merged fix rounds: ids one case fixes together interact): `STYLEBRO_WRITE_INTERACTIONS=1 dotnet test
  --filter FixInteractionsTests` writes it, FixInteractionsTests fails until then.
- Analyzers: whole-tree walks use `TreeWalk.Tokens/Trivia/Nodes(root)`; syntax-only analyzers use
  `context.RegisterNodeAction` (NodeActions.cs), not `RegisterSyntaxNodeAction` (node/symbol actions make the driver bind
  every method body); cheap syntax checks before semantic work. Severity keys (`dotnet_diagnostic.X.severity`) aren't in
  AnalyzerConfigOptions: read them from `CompilationOptions.SyntaxTreeOptionsProvider` (`Severities.IsOn`). A doc comment
  trivia's `Span` starts after its first `///`: use `FullSpan.Start`. An `SAxxxx` in a rule's description makes
  `Migration.ExpandIds` treat it as a StyleCop replacement.
- Fixes: must only read the solution (Fix All runs documents in parallel) and keep shared caches thread-safe; a fix that
  reads solution-wide data asks with the solution it started from, never a forked document's (BRO1112's Fix All went to
  387 s that way). Never choose an edit by target framework or `#if` (linked copies would get different fixes).
- Renames: compiling is not enough, run the repositories' TESTS (reflection, serializers, snapshots, run-time names).
  A single clean real-world run proves little: repeat it.
- `$$` in unit-test markup is the caret position (raw `$$"""` strings are tested in samples/Messy only). Unit tests can't
  parse C# 14 (Roslyn 4.8): samples/Messy covers it.
- `--` inside an XML comment breaks a targets file (MSB4024). Keep probe backups outside the probe project (a copy inside
  got rewritten too). `<WarningsNotAsErrors>BRO1001</WarningsNotAsErrors>` makes `dotnet format` skip the rule.
- Real-world `-Work` folders go OUTSIDE the repo tree (inside, the repo's props leak into the clones) and unique per
  agent; `.trials/` is the exception (stub props, gitignored). `-Enable A,B` must reach the script as a list; confirm the
  rule really reports.

## Where to find details (docs/notes/, read on demand)

- `docs/notes/rules.md`: per-rule implementation notes (BRO1001, BRO11xx readability, BRO13xx naming, BRO16xx
  documentation, BRO15xx layout): logic, skips, parity, deviations, bugs found. Add new rule notes there.
- `docs/notes/migration.md`: `bro migrate` (reading StyleCop/StyleSharp setups, mapping, suppressions, measuring),
  baseline support, the preset packaging bug and `bro init`, the first runs through the package.
- `docs/notes/format.md`: the `bro format` investigation (per-framework runs, convergence, speed on large repositories)
  and the xUnit fixes it applies.
- `docs/notes/performance.md`: build timings, the benchmark, the performance fixes and the 2026-10-22 audit fixes.
- `docs/notes/real-world-log.md`: real-world results per rule batch on the reference repositories (2026-09-29 onwards).
- `docs/notes/trials.md`: every first-run trial (repos, fixes, owner's verdict, tally, environment). **A new trial adds
  its `## Trial NN` section at the end of docs/notes/trials.md, not here.**
- `docs/notes/history.md`: the old release history and the dated "Added 2026-10-04 ... 10-10" sections (rules, guards,
  speed rounds, CI changes, lessons).
- Rule pages (`docs/rules/BROxxxx.md`), `docs/decisions/` and the generated backlog stay the primary references.

## Known open questions

- Answered: `dotnet format` does pick up code fixes from analyzers referenced as
  `ProjectReference OutputItemType="Analyzer"` (as in `samples/Messy`).
- Answered: help links and `PackageProjectUrl` point to `github.com/bisforboman/stylebro`.
- Name checked 2026-09-29: NuGet IDs `StyleBro` and `StyleBro.Analyzers` are free (0 search hits);
  a `stylebro` GitHub user/org is free; no similar C# projects on GitHub. `StyleBro.Analyzers` is now taken by us.

## Next steps

`docs/backlog.md` is the list (generated by `python scripts/New-Backlog.py` from `scripts/backlog/`; on the `generated`
branch, not committed to main). StyleCop parity is complete (every rule is StyleBro, SDK, or dropped by design with a reason in
`docs/skipped-rules.md`). Work in progress and decisions go through PRs and `docs/decisions/`.
