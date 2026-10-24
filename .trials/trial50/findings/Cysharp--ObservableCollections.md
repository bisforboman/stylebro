# Trial 50 runner: Cysharp/ObservableCollections

- Repository: https://github.com/Cysharp/ObservableCollections, commit `8ed7ad87b35885b2540158d9188d710b1cf32763` (random pick 2 of 3, seed 50).
- StyleBro: `claude/trial-50` at `26e9eb80`, packed as `0.8.0-trial50.1` (StyleBro.Analyzers + StyleBro.Tool) into `/home/user/feed50`,
  added as a global NuGet source; `dotnet tool install -g StyleBro.Tool --version 0.8.0-trial50.1`, `ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`.
- SDKs: 10.0.113 and 8.0.132 (Ubuntu packages), pwsh at /usr/local/bin: all preinstalled.
- Setup of the repository: 8 projects in `ObservableCollections.slnx` (2 libraries multi-targeted netstandard2.0;netstandard2.1;net6.0;net8.0,
  2 test projects net6.0/net8.0, Avalonia/Blazor/Console/WPF samples); `sandbox/WinUI3App` isn't in the solution. No StyleCop, no
  TreatWarningsAsErrors, no nuget.config, no global.json; 63 C# files. So: `bro init`, and TWAE builds forced with `-p:TreatWarningsAsErrors=true`.
- Environment: `NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major CI=true DiffEngine_Disabled=true`, `-p:NuGetAudit=false
  -p:EnableWindowsTargeting=true` (WpfApp is net6.0-windows) on builds and tests (`ObservableCollections/env.sh`).

## Commands

Clones: `base` (untouched), `default`, `host`, all at the commit above, under `.trials/trial50/ObservableCollections/`.

```
# baseline (base)
dotnet build ObservableCollections.slnx -c Debug $P
dotnet build ObservableCollections.slnx -c {Debug,Release} --no-incremental $P -p:TreatWarningsAsErrors=true
dotnet test ObservableCollections.slnx -c {Debug,Release} $P

# default engine (default); host engine (host) the same with --engine host on format/check
bro preview init
bro init --write
# + <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial50.1" PrivateAssets="all" /> in Directory.Build.props
#   (the same lines the preview adds), commit
bro format --timings                 # host: bro format --engine host --timings
bro check                            # host: bro check --engine host
bro check --json
bro check --build
bro check --build --json
bro format --once                    # default only: a second process changes nothing
dotnet build ObservableCollections.slnx -c {Debug,Release} --no-incremental $P -p:TreatWarningsAsErrors=true [-p:EnforceCodeStyleInBuild=true]
dotnet test ObservableCollections.slnx -c {Debug,Release} $P
# negative check: undo one BRO1514 fix in src/ObservableCollections/RingBuffer.cs, bro check [--engine host], restore
# host extras: bro preview format --engine host; bro format --engine host --diff; bro preview format; bro preview init --engine host
```

## Results

### Default engine

| Command | Verdict | Exit | Time |
|---|---|---|---|
| `bro preview init` | 45 files, 411 changes, clean after 1 run; Not checked: IDE0049 (SDK note) | 0 | 1m35s |
| `bro init --write` | 74 lines in .editorconfig, `<StyleBroPreset>1.0</StyleBroPreset>` | 0 | 4.7s |
| `bro format` | Run 1: 45 files changed; Run 2: 0 files changed, clean; Not checked: IDE0049 | 0 | 1m52s |
| `bro check` | Clean. (Not checked: IDE0049) | 0 | 28s |
| `bro check --json` | `clean: true, exitCode: 0, notChecked: [IDE0049]` | 0 | 30s |
| `bro check --build` | Debug/Release: no findings, no new errors. Clean. | 0 | 52s |
| `bro check --build --json` | as `check --json` | 0 | 50s |
| `bro format --once` (2nd process) | Run 1: 0 files changed, clean | 0 | - |
| negative `bro check` (one unbraced `if`) | Not clean: formatting would change 1 file | 2 | - |

### Host engine

| Command | Verdict | Exit | Time |
|---|---|---|---|
| `bro preview init` (no `--engine`: preview init rejects it, "Unknown option: --engine", exit 1) | identical patch to the default clone's (`cmp`) | 0 | 1m28s |
| `bro init --write` | identical output and .editorconfig | 0 | 4.6s |
| `bro format --engine host` | Run 1: 45 files changed; Run 2: 0 files changed, clean; Not checked: IDE0049 | 0 | 50s |
| `bro check --engine host` | Clean. (Not checked: IDE0049) | 0 | 21s |
| `bro check --engine host --json` | identical JSON to the default engine's (path aside) | 0 | 21s |
| `bro check --build --engine host` | Debug/Release: no findings, no new errors. Clean. | 0 | 46s |
| `bro check --build --engine host --json` | identical JSON to the default engine's | 0 | 44s |
| negative `bro check --engine host` | Not clean: formatting would change 1 file | 2 | - |
| `bro preview format --engine host` | **The preview failed: 'bro format' failed** (finding 1) | 1 | 10s |

Engines compared:

- Trees after format: `diff -r -x .git -x bin -x obj host default`: identical, byte for byte. The preview patch applied to the
  untouched code also gives exactly the default clone's tree (`.editorconfig` and Directory.Build.props included).
- Format output: identical up to `Timings`, except the order of the `Checked <tfm> (run i of 5)` lines (completion order).
  No load note (PR #287) appeared on this repository. Both print "'dotnet format' had warnings loading the workspace in its
  netstandard2.0 run".
- `Not checked:` lines: the same single IDE0049 line (SDK 10.0.113) in every format/check of both engines.
- `--json`: `check` and `check --build` identical in both engines.

### Builds and tests

| Tree | TWAE Debug | TWAE Release | TWAE + EnforceCodeStyleInBuild | Tests Debug | Tests Release |
|---|---|---|---|---|---|
| untouched | 0 errors, 4 warnings (NETSDK1138, net6.0 out of support) | same | - | 24 + 10 passed | 24 + 10 passed |
| default engine | same | same | 0 errors, 4 NETSDK1138 | 24 + 10 passed | 24 + 10 passed |
| host engine | same | same | - | 24 + 10 passed | 24 + 10 passed |

All 14 assemblies built (`--no-incremental`). Verdicts match the builds and tests: Clean, and nothing in the builds or tests
contradicts it; format converged in one changing run (run 2 and a separate `--once` process change nothing); no test
regressed.

The fixes, read by hand (src/ and tests/, `git diff -w --ignore-blank-lines`): braces (BRO1514), `else { if }` -> `else if`
(BRO1139), three nested `if`s merged into one condition in `ObservableDictionary.Remove(KeyValuePair)` (`out var` kept in
scope), nested private classes `_DictionaryCollectionAdd/Remove/Replace` -> PascalCase, internal tuple element names
`(T value, TView view)` -> `(T Value, TView View)` with their uses, trailing comments after `foreach (...)`/`if (...)` moved
into the block, usings sorted alphabetically (the repository's own order: `dotnet_sort_system_directives_first = false`
kept), `is null` -> `== null` (the repository's majority, kept by init). Nothing semantic looked wrong; the tests agree.

## Findings: 1

### 1. `bro preview format --engine host` (and `bro format --engine host --diff`) fails: `--engine host` is passed to `dotnet format`

What happened: the preview runs format on its copy through `FormatCommand.RunPlain` (src/StyleBro.Migrate/PreviewCommand.cs:96),
which goes straight to `RunOnce`; `--engine` is only taken out of the arguments in `FormatCommand.Run`
(src/StyleBro.Migrate/FormatCommand.cs:285, `TakeEngine`). So the option reaches `dotnet format`:

```
Format run 1... 10s
The preview failed: 'bro format' failed (exit code 1).
The end of its log:
  Description:
    Formats code to match editorconfig settings.
  ...
# preview.log:
Unrecognized command or argument '--engine'.
Unrecognized command or argument 'host'.
```

The other preview forms are inconsistent with it: `bro preview init --engine host` says `Unknown option: --engine` (exit 1,
clean message), `bro check --engine bogus` names the allowed values, and `bro preview format` without the option works (exit 0,
0-line patch on the formatted tree). docs/format-engine.md only names `bro check`/`bro format`, but the help says `bro preview
format ... [options]` and `bro format ... --diff` is the same command, so a user trying the host engine on a preview gets
dotnet-format's usage text. Not a wrong verdict (exit 1, "The preview failed"), only a confusing failure.

Smallest reproduction (`.trials/trial50/spot/preview-engine`, log `ObservableCollections/logs/spot-preview-engine.log`):

```
dotnet new classlib -n Lib -o . && git init && git add -A && git commit -m x
bro preview format --engine host      # exit 1, dotnet-format usage; preview.log: Unrecognized command or argument '--engine'
```

Expected: either the preview honors `--engine host` (runs its format runs in the host) or rejects it up front with the same
kind of message as `preview init` / `--engine bogus`.

Logs: `ObservableCollections/logs/host-1b-preview-format.log`, `host-preview-diff-engine.log`, `spot-preview-engine.log`.

## Observations (not findings)

- `bro check --json` reports `"command": "format"` (check is `format --verify-no-changes`); same in both engines.
- init kept the repository's file-level mix: `charset = unset` (44 of 63 files have a BOM) and `insert_final_newline = unset`
  (47 of 63), both explained in its notes.

Logs: `.trials/trial50/ObservableCollections/logs/` (`base-*`, `default-*`, `host-*`, `default-format.diff`).

## Re-check (fixer's 1a25ead, PR #294)

- StyleBro `claude/trial-50` at `1a25ead`, packed as `0.8.0-trial50.2`, `dotnet tool update -g StyleBro.Tool --version 0.8.0-trial50.2`.
- Fresh clones `r-default` and `r-host` of `base` at 8ed7ad87, one engine after the other, scripted in
  `ObservableCollections/recheck.sh <default|host>` (host passes `--engine host` to every format/check/preview format command).
  Logs: `ObservableCollections/logs/recheck/`.

| Step | Default: exit, time | Host: exit, time |
|---|---|---|
| `bro preview init` | 0, 118 s (45 files, clean after 1 run) | 0, 90 s (same) |
| `bro init --write` (+ package reference 0.8.0-trial50.2, commit) | 0, 5 s | 0, 5 s |
| `bro preview format [--engine host] --diff=<file>` (init'd tree) | 0, 92 s, 2,299-line patch | 0, 44 s, the same patch (`cmp`) |
| `bro format [--engine host] --diff=<file>` (init'd tree) | 0, 88 s, the same patch | 0, 44 s, the same patch |
| `bro format [--engine host]` | 0, 113 s: run 1 45 files, run 2 clean | 0, 47 s: the same |
| `bro format --once` again | 0: 0 files changed | 0: 0 files changed |
| `bro check` | 0, Clean | 0, Clean |
| `bro check --json` | 0, `clean: true, exitCode: 0` | 0, identical JSON |
| `bro check --build` | 0, Clean (Debug, Release: no findings, no new errors) | 0, the same |
| `bro check --build --json` | 0 | 0, identical JSON |
| `bro preview format` / `bro format --diff` (formatted tree) | 0 / 0, empty patches | 0 / 0, empty patches |
| TWAE Debug / Release | 0 errors, 4 NETSDK1138 | 0 errors, 4 NETSDK1138 |
| Tests Debug / Release | 24 + 10 passed | 24 + 10 passed |

- **Finding 1 fixed:** `bro preview format --engine host` and `bro format --engine host --diff` now exit 0. They take half the
  default's time (44 s vs 88-92 s), so the copy is formatted by the host. Their patch is identical to the default engine's.
  Applied to the init'd tree (`git apply` on HEAD~1), the patch gives exactly the formatted tree, in both clones. On the
  formatted tree it is empty.
- The trees after format are identical between the engines, and identical to round 1 apart from the package version in
  Directory.Build.props. Format's output is the same apart from the order of the `Checked` lines. The `Not checked:` lines are
  the same single IDE0049 line in all 18 logs. The `--json` output is identical, and every exit code is 0.
- The builds and tests match the untouched code. Format converges in one changing run. No new findings.

### Final pass (4c60b3c)

- I started on 990c775, packed as 0.8.0-trial50.3. The default engine finished there with all 14 steps at exit 0 (logs:
  `logs/recheck2-990c775-partial/`). Then the fixer moved the pass to 4c60b3c, which adds f09f8c8: a multi-targeted project's
  additional files now come from each framework's evaluation. This repository has two multi-targeted libraries, so I redid
  the whole pass there.
- StyleBro 4c60b3c, packed as `0.8.0-trial50.4`, run with `bro --version` = 0.8.0-trial50.4. Fresh clones `f-default` and
  `f-host` of `base`, made by `V=0.8.0-trial50.4 LOGDIR=recheck3 PFX=f recheck.sh <engine>`. Logs: `logs/recheck3/`.

| Step | Default: exit, time | Host: exit, time |
|---|---|---|
| `bro preview init` | 0, 115 s (45 files, clean after 1 run) | 0, 100 s (same) |
| `bro init --write` | 0, 6 s | 0, 5 s |
| `bro preview format [--engine host] --diff=<file>` | 0, 110 s, 2,299 lines | 0, 49 s, identical patch |
| `bro format [--engine host] --diff=<file>` | 0, 101 s, identical patch | 0, 48 s, identical patch |
| `bro format` | 0, 125 s: run 1 45 files, run 2 clean | 0, 54 s: the same |
| `bro format --once` again | 0: 0 files changed | 0: 0 files changed |
| `bro check` / `--json` | 0, Clean / `clean: true, exitCode: 0` | 0, Clean / identical JSON |
| `bro check --build` / `--json` | 0, Clean / exit 0 | 0, Clean / identical JSON |
| preview format / format --diff on the formatted tree | 0 / 0, empty patches | 0 / 0, empty patches |
| TWAE Debug / Release | 0 errors | 0 errors |
| Tests Debug / Release | 24 + 10 passed | 24 + 10 passed |

- **Unchanged:**
  - The trees after format are identical between the engines, and identical to the 1a25ead re-check apart from the package
    version.
  - The preview and `--diff` patches are byte-identical to the 1a25ead ones, which I had already shown apply to the init'd
    tree and give the formatted tree.
  - Format's output is identical apart from the order of the `Checked` lines. All 18 `Not checked:` lines are the single
    IDE0049 line. The `--json` output is identical.
  - Builds and tests match the untouched code.
- No new findings. Finding 1 stays fixed.
