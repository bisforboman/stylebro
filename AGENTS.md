# StyleBro

Instructions for AI coding agents working on this repository (Claude Code reads them through CLAUDE.md, GitHub Copilot
through .github/copilot-instructions.md). Using StyleBro in another repository instead: plugins/stylebro/skills/stylebro/SKILL.md
and docs/agents.md.

Roslyn analyzers and code fixes for C#: a modern replacement for StyleCop.Analyzers (stuck in 1.2.0-beta for years).
Rule prefix `BRO`, config keys `stylebro_*`. Repo: `github.com/bisforboman/stylebro`.

**Core idea**: only analyzers with code fixes that `dotnet format` can apply, so that running `dotnet format` (or
`bro format`) fixes everything StyleBro reports. Formatting-only rules are out of scope where the SDK already covers
them (IDE rules, `.editorconfig`); the preset turns those built-in rules on instead.

| Topic | Where |
|---|---|
| Layout, build and test, what every rule needs, PR checks, changelog, backlog, merge conflicts | CONTRIBUTING.md |
| Releases (history and notes per version) | CHANGELOG.md; setup behind release.yml: docs/notes/release.md |
| Versioning, preset versions, styles, when 1.0 ships | docs/versioning.md, docs/proposals/preset-versions.md, docs/styles.md |
| The in-process format engine (the default) and `--engine dotnet-format` | docs/format-engine.md |
| `bro` for scripts and agents (`--json`, `format --files`, `--agents-md`, `--skill`) | docs/agents.md |
| StyleBro's skill for agents (the plugin marketplace, embedded by `--skill`, published on the site) | plugins/stylebro/skills/stylebro/SKILL.md, .claude-plugin/marketplace.json |
| Design questions and answers | docs/decisions/ (`pwsh scripts/New-Decision.ps1 -Title "..."`) |
| Per-rule implementation notes (logic, skips, parity, bugs) | docs/notes/rules.md (add new rule notes there) |
| `bro migrate`, baselines, `bro init` | docs/notes/migration.md |
| `bro format`'s investigation; timings and performance; real-world results | docs/notes/format.md, performance.md, real-world-log.md |
| First-run trials (repos, fixes, verdicts, tally, environment) | docs/notes/trials.md (a new trial's `## Trial NN` goes at its end), scripts/trials/README.md |
| The fixture matrix | scripts/fixtures/README.md |
| StyleCop inventory, mapping, skipped rules | scripts/stylecop-survey/README.md, docs/stylecop-mapping.md, docs/skipped-rules.md |
| Older status, release history, dated lessons | docs/notes/history.md |

## Status and release policy

- 1.0 ships after two clean first-run trials in a row (docs/decisions/2026-10-11-when-stylebro-goes-stable.md). The
  tally is kept in scripts/backlog/work/072-two-clean-trials-before-10.json and each trial's verdict in
  docs/notes/trials.md; recent trials were not clean (0 in a row after trial 64).
- Releases: the owner says when. Don't suggest releases or push release tags. The newest `## X.Y.Z` in CHANGELOG.md is
  the latest release; `StyleBroSelfVersion` in Directory.Build.props is the release the self-check uses.

## Working on this repository

- **Asking the owner**: whenever work needs the owner's input (a design choice, a Windows-only step, an approval), ask
  with your agent's question tool, never only in prose, a PR body or a log; carry on with what doesn't depend on the
  answer.
  Record design decisions in docs/decisions/.
- **Where work runs**: long work (PRs, CI waits, releases) runs as cloud sessions; the owner's computer must stay free. Work that needs it (local timings, Windows-only checks) is asked for, never started unasked;
  a PR that needs such a step says so.
- **Before pushing**: `dotnet test StyleBro.slnx -p:StyleBroSelf=none`, `pwsh scripts/verify-format.ps1`, and the
  self-check `dotnet format StyleBro.slnx --verify-no-changes --severity warn --exclude samples` (CI runs all three,
  plus `pwsh scripts/verify-package.ps1` and the pack). One test: `dotnet test StyleBro.slnx -p:StyleBroSelf=none
  --filter FullyQualifiedName~<Class>`.
- **PR pipeline**: `main` is protected. Branch, push, `gh pr create`, merge when green. Required checks: `build`,
  `parity`, `performance` (ci.yml), `mutation`, `fixtures` (docs/decisions/2026-10-27-the-fixture-matrix-is-a-required-check-on-src-prs-and.md)
  and one `Real world (<repo>)` per repo in scripts/realworld/repos.psd1. What each checks: CONTRIBUTING.md.
- **Scoped runs**: `.github/changes.sh` decides. A docs-only PR skips the heavy work, but every required check still
  reports: skip with step-level `if`s (a job-level `if` on a matrix job hides its check names). Fixtures and
  performance run only when code or build settings change.
- **Nightly on main** (not after every merge): real world, mutation (all entries), fixtures and drift. A failure
  opens or comments on one issue per streak (`.github/nightly-issue.sh`). Dispatch any of them on a branch to run it there.
- **Mutation**: every new guard gets an entry, one file each: `scripts/mutation/New-Mutation.ps1 -File -Find -Replace
  -Tests [-Comment]` (CONTRIBUTING.md). `Tests` must not start with `FullyQualifiedName~` (the runner adds it) and must
  be narrow. On PRs only the entries of the changed files run (`-OnlyFiles`); the stale check (every entry's `Find`
  occurs once in its `File`) always covers all. Run it in a separate worktree, after committing: it undoes each
  mutation with `git checkout <file>`.
- **Fixtures**: `pwsh scripts/fixtures/Invoke-Fixtures.ps1 [-Shape a,b] [-Keep]` (3-5 minutes a shape; in a cloud
  container set `NUGET_CERT_REVOCATION_MODE=offline` first). A trial bug in a new kind of repository gets a shape.
- **Drift**: drift.yml runs `pwsh scripts/benchmark/Test-Drift.ps1`: main's analyzers against the last release, so
  slowdowns under the per-PR `performance` threshold can't add up unnoticed.
- **Other workflows**: sonar-interop.yml (weekly) checks the fixes for Sonar's S1066/S2971/S3878 against the real
  SonarAnalyzer package (`scripts/sonar-interop.ps1`); dependabot-auto-merge.yml turns on auto-merge for Dependabot's
  minor and patch PRs; vulnerable-packages.yml (weekly) lists vulnerable NuGet packages, direct or transitive, and opens
  or comments on an issue (every restore already fails on one: Directory.Build.props; fix with a pin that names the
  advisory, never a suppression; docs/decisions/2026-11-06-vulnerable-packages-fail-every-build.md); codeql.yml (pull
  requests to main but docs-only ones, pushes to main, weekly) is GitHub's code scanning of the C#, not required.
- **Security analyzers**: the SDK's Security rules (CA2xxx, CA3xxx, CA5xxx) all run as errors on every build of
  StyleBro's own projects, not samples/ or scripts/fixtures/ (Directory.Build.props). Fix a real finding; suppress a
  false positive where it is, with a justification (docs/decisions/2026-11-06-security-analyzers-and-codeql.md).
- **Fewer conflicts** (docs/decisions/2026-10-18-fewer-pr-conflicts.md): a trial opens one PR (if it must split, the PR
  touching the shared files merges first, the others merge main once after it). Merge main into a PR only when GitHub
  keeps showing a real conflict (DIRTY several times) or CI needs main's code; after any merge, build, rerun the tests
  and the mutation stale check before pushing. merge=union files (`.gitattributes`) can double or resurrect lines:
  check them.
- **Changelog**: a change users notice adds `changes/<branch>.md` (a `/` in the branch is a folder), never CHANGELOG.md.
  Short, concrete, no internal jargon, never private-app details.
- **Release** (when the owner says): once every PR going into it has merged, one PR runs `python scripts/changelog.py
  X.Y.Z-alpha.N "<summary>"` (folds the fragments, adds "Upgrading from <previous>"), moves AnalyzerReleases Unshipped
  to Shipped, bumps the versions in both csproj files and the docs and samples, and drops the docs' *Next release*
  markers. Merging it releases (release.yml). Then bump `StyleBroSelfVersion` in a PR of its own.
- **Backlog**: `scripts/backlog/work/<NNN>-<slug>.json` (a new item takes the next free number); `python
  scripts/New-Backlog.py` renders it (not committed; published to the `generated` branch). Update an item's status
  when work starts or lands.
- **Docs site**: `mkdocs build --strict` (docs.yml). A new page needs a `nav` entry, except under the excluded
  docs/notes/, docs/decisions/, docs/proposals/. docs/getting-started.md keeps the path (install, A, B, first run): new
  detail goes into its reference pages. docs/configuration.md lists every `stylebro_*` key (a test checks it).
- **StyleCop coverage**: every StyleCop rule StyleBro doesn't cover has an entry in scripts/stylecop-survey/skipped.psd1;
  when one is implemented, move it to decisions.psd1.
- Line endings: C# files under src/ and tests/ are LF; keep new edits LF.

## Design rules for every rule

The details are in CONTRIBUTING.md, "What every rule needs".

1. Every diagnostic has a code fix and Fix All works: `LinkedFileFixAllProvider.Create(FixDocumentAsync)`, never plain
   `FixAllProvider.Create` (linked copies of a multi-targeted file got conflict markers). Skip decisions that depend on
   `#if`-conditional code.
2. Fixes are deterministic and idempotent: afterwards the analyzer reports nothing and a second run changes nothing,
   in any fix order. Analyzer and fix share one piece of logic.
3. When a fix could be unsafe, skip the case: no diagnostic rather than a wrong fix.
4. Configuration goes through `.editorconfig` keys `stylebro_*`, read via `AnalyzerConfigOptionsProvider` in both the
   analyzer and the fix.
5. Docs and tests: a rule page, its index rows, unit tests for the single fix and Fix All, a samples/Messy case, a
   mutation entry per guard. The docs describe StyleBro, never compare it with StyleCop: a rule page names StyleCop
   only in its one `Replaces StyleCop **SAxxxx**.` line (test-enforced;
   docs/decisions/2026-11-07-the-docs-describe-stylebro-not-stylecop.md).

## Layout

CONTRIBUTING.md has the tree. What it doesn't say:

- `src/StyleBro.Analyzers` must never reference Workspaces (RS1038). Roslyn stays pinned to 4.8.0; don't raise it
  without a reason. Package versions are central (Directory.Packages.props).
- `src/StyleBro.Package`: `build/StyleBro.Analyzers.targets` adds the preset layers
  `build/presets/stylebro.X.Y.globalconfig` (global_level -1) up to `<StyleBroPreset>`. One layer per minor, holding
  only that minor's new rules and keys (a key in two layers conflicts). **Rules added after 1.0 MUST be
  `isEnabledByDefault: false`**: only the preset turns them on, so `none` and migrated repos stay quiet. A rule's
  version is its AnalyzerReleases.Shipped.md header (pre-1.0 = 1.0; Unshipped = `PresetVersions.Latest`). A minor's
  first new rule: add `presets/stylebro.1.1.globalconfig`, its line in the targets, raise `_StyleBroPresetLatest` and
  `PresetVersions.Latest` (PresetVersionTests checks it all). StyleBro's own code uses `latest`.
- `src/StyleBro.Migrate`: the `bro` tool (package StyleBro.Tool; the project, the assembly and markers in users' files
  keep `stylebro-migrate`). `bro check` = `format --verify-no-changes`, `bro preview <cmd>` = `--diff` (`Program.Expand`).
  It embeds the StyleCop survey's CSVs and reads each rule's "Replaces StyleCop SAxxxx" from its description.
- `src/StyleBro.FormatHost`: the in-process engine `bro format` and `bro check` use by default (docs/format-engine.md).
  Its `FixInteractions.txt` must be rewritten for a new rule or FixOrderTests case: `STYLEBRO_WRITE_INTERACTIONS=1
  dotnet test --filter FixInteractionsTests`.
- `samples/Messy`, `samples/MultiTarget`: verify-format copies `Input/` to `Generated/`, formats, compares with
  `Expected/`, then a second run must change nothing (MultiTarget must also build).
- **The self-check** uses the latest release (`StyleBroSelfVersion`): a PackageReference in Migrate and Tests, a
  `PackageDownload` that Directory.Build.targets adds for Analyzers and CodeFixes (a reference would be a NU1108 cycle).
  `StyleBroSelf=none` turns it off; verify-format.ps1 sets it, or a sample's format loads two fixers per rule and may
  test the release instead of the code.
- `scripts/`: verify-package checks what only the package's targets do, in a project outside the repo. realworld:
  `Invoke-RealWorld.ps1 -Tests`, `MaxRuns` in repos.psd1 for a documented exception. stylecop-survey is not in the slnx.

## Cloud sessions (environment)

- .NET SDK 10.0.1xx and 8.0.1xx come from Ubuntu's packages (builds.dotnet.microsoft.com is blocked by the proxy). pwsh:
  `dotnet tool install -g PowerShell` (link it into /usr/local/bin if missing); a `bro` installed as a tool needs
  `ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`.
- The container runs as root: `Migrate_WithALockedFile_WritesNothing` fails there (and on main). SDK 10.0.1xx's
  `dotnet format` style pass loads no DiagnosticSuppressors: verify-package.ps1 check 5 fails locally, passes in CI on
  the newest SDK 10.
- Restores through the proxy: `NUGET_CERT_REVOCATION_MODE=offline` (NU3018, also for `dotnet test`); `-p:NuGetAudit=false`
  where the untouched code fails NU190x under TreatWarningsAsErrors. A repo whose nuget.config `<clear/>`s gets the trial
  feed (and a `StyleBro.*` packageSourceMapping) in that file; lower a global.json pinning a newer SDK; no .NET 9
  runtime: `DOTNET_ROLL_FORWARD=Major`. More per-repo recipes: each trial's Environment bullet in docs/notes/trials.md.
- About 15 GB of memory: run heavy jobs one at a time (OOM kills show as exit 137 or "Test host process crashed").
  Worker restarts kill background jobs: start long reruns with `setsid nohup`. `pkill -f`/`pgrep -f` with a pattern from the same command line matches the calling shell.
- Trial clones are third-party: never push them. Delete unused clones in `.trials/`: their `*.verified.txt` files make
  verify-format and snapshot tests fail or spin.
- In a bash heredoc Python script `\\n` became a real newline: write such scripts with the Write tool.

## Gotchas (the history behind each is in docs/notes/)

- After merging main: build before trusting mutation results (a compile error "kills" every mutation). Agents squashing
  onto a moved main can revert other PRs' changes: check a PR's file list.
- Fix order under `dotnet format` is not fixed. A fix that changes a node's shape must also produce what other rules
  want for it; ask another rule's configured decision. A layout rule gets a `FixOrderTests` case (it never sees the
  whitespace pass) and several verify-format runs; compare preview and format output file by file.
- Analyzers: whole-tree walks use `TreeWalk.Tokens/Trivia/Nodes(root)`; syntax-only analyzers use
  `context.RegisterNodeAction`, not `RegisterSyntaxNodeAction` (which makes the driver bind every method body); cheap
  syntax checks before semantic work. Severity keys (`dotnet_diagnostic.X.severity`)
  aren't in AnalyzerConfigOptions: read them with `Severities.IsOn`. A doc comment trivia's `Span` starts after its
  first `///`: use `FullSpan.Start`. An `SAxxxx` in a rule's description makes `Migration.ExpandIds` treat it as a
  StyleCop replacement.
- Fixes only read the solution (Fix All runs documents in parallel), keep shared caches thread-safe, and ask
  solution-wide questions of the solution they started from, never a forked document's (BRO1112's Fix All took 387 s).
- Renames: compiling is not enough, run the repositories' tests. One clean real-world run proves little: repeat it.
- `$$` in unit-test markup is the caret. Unit tests can't parse C# 14 (Roslyn 4.8): samples/Messy covers it.
- `--` inside an XML comment breaks a targets file (MSB4024).
  `<WarningsNotAsErrors>BRO1001</WarningsNotAsErrors>` makes `dotnet format` skip the rule.
- Real-world `-Work` folders go outside the repo tree (inside, the repo's props leak into the clones), unique per agent;
  `.trials/` is the exception (stub props, gitignored). `-Enable A,B` must reach the script as a list; confirm the rule
  really reports.
