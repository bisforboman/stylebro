# Trial 50 runner: dahomey-technologies/Dahomey.Json

- Repository: https://github.com/dahomey-technologies/Dahomey.Json at `43ddbf14d282eccb9802a040fc5d85e7a1422166` (random pick 3 of 3, seed 50).
- StyleBro: branch `claude/trial-50` at `26e9eb8`, packed as `0.8.0-trial50` (StyleBro.Analyzers + StyleBro.Tool) into
  `/home/user/feed50`, added as a global NuGet source; tool installed with
  `dotnet tool install -g StyleBro.Tool --version 0.8.0-trial50 --add-source /home/user/feed50` and linked into /usr/local/bin.
- SDKs: 10.0.113 and 8.0.132 (Ubuntu packages), pwsh: both preinstalled.
- Setup: no StyleCop/Sonar/CSharpier, no global.json, no nuget.config, no .editorconfig, no Directory.Build.props. Solution
  `src/Dahomey.Json.sln`: library `netstandard2.0;netcoreapp3.1;net5.0;net6.0` (has `TreatWarningsAsErrors=True` itself),
  tests `netcoreapp3.1;net5.0;net6.0`. 160 .cs files.
- Environment: `NUGET_CERT_REVOCATION_MODE=offline DOTNET_ROLL_FORWARD=Major`, `-p:NuGetAudit=false` on builds.
- Clones: `.trials/trial50/Dahomey.Json/{base,default,host}` (base = untouched, kept for the re-check).

## Commands

```bash
# baseline (base/), and the same after format in default/ and host/   (.trials/trial50/Dahomey.Json/build.sh)
dotnet build src/Dahomey.Json.sln -c Debug   --no-incremental -p:TreatWarningsAsErrors=true -p:NuGetAudit=false
dotnet build src/Dahomey.Json.sln -c Release --no-incremental -p:TreatWarningsAsErrors=true -p:NuGetAudit=false
dotnet test  src/Dahomey.Json.sln -c Release --no-build

# default engine (default/)
bro preview init
bro init --write
# + <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial50" PrivateAssets="all" /> in Directory.Build.props
#   (getting-started step; the same lines the preview patch adds)
bro format
bro check ; bro check --json
bro check --build ; bro check --build --json
bro preview format ; bro preview format --engine dotnet-format

# host engine (host/), same commit
bro preview init --engine host
bro init --write   (+ the same PackageReference)
bro format --engine host
bro check --engine host ; bro check --engine host --json
bro check --build --engine host ; bro check --build --engine host --json
bro preview format --engine host ; bro format --diff --engine host
```

## Results

| Command | Default: verdict | exit | time | Host: verdict | exit | time |
|---|---|---|---|---|---|---|
| `bro preview init` | 92 files, 338 changes, clean after 1 run | 0 | 96 s | `--engine`: "Unknown option" (preview init has no engine) | 1 | 0 s |
| `bro init --write` | wrote .editorconfig (69 lines), Directory.Build.props (preset 1.0) | 0 | 3 s | identical output and files | 0 | 3 s |
| `bro format` | run 1: 92 files; run 2: 0, clean | 0 | 104 s | run 1: 92 files; run 2: 0, clean | 0 | 56 s |
| `bro check` | Clean | 0 | 21 s | Clean | 0 | 20 s |
| `bro check --json` | clean true, exitCode 0 | 0 | 22 s | identical apart from `path` | 0 | 20 s |
| `bro check --build` | Clean (Debug, Release: 15 untouched errors not counted) | 0 | 50 s | Clean, same lines | 0 | 50 s |
| `bro check --build --json` | clean true, exitCode 0 | 0 | 50 s | identical apart from `path` | 0 | 48 s |
| `bro preview format` (after format) | nothing to change | 0 | 43 s | `--engine host`: **failed** (finding 1) | 1 | 8 s |
| `bro preview format --engine dotnet-format` | **failed** (finding 1) | 1 | 7 s | | | |
| `bro format --diff --engine host` | | | | **failed** (finding 1) | 1 | 8 s |

`Not checked` on every format/check, both engines, word for word: `IDE0049: this SDK's 'dotnet format' ignores the
multi-target guard, so these fixes are held back and not checked (a note: ...; the verdict doesn't count them)` (SDK
10.0.113, expected). No load note from the host engine on this repository (PR #287's note did not appear).

Engine comparison:

- Trees after format: `diff -rq --exclude=.git --exclude=bin --exclude=obj default host`: **identical**.
- Format logs differ only in the order of the "Checked <tfm> (run i of 4)" lines.
- `--json` of check and check --build: equal apart from `path`.
- Preview vs format: the default preview patch applied to a fresh clone gives exactly the formatted tree (`diff -rq`: identical).

Builds and tests (TWAE, `--no-incremental`):

| | Debug | Release | Tests netcoreapp3.1 / net5.0 / net6.0 |
|---|---|---|---|
| Untouched | 0 warnings, 15 errors | 0 warnings, 15 errors | 360 / 372 / 374 passed, 0 failed |
| Default engine after format | 0 warnings, 15 errors | 0 warnings, 15 errors | 360 / 372 / 374 passed, 0 failed |
| Host engine after format | 0 warnings, 15 errors | 0 warnings, 15 errors | 360 / 372 / 374 passed, 0 failed |

The 15 errors are the untouched code's own: `CS0122 'MaybeNullAttribute'/'NotNullWhenAttribute'/... is inaccessible`
in the netstandard2.0 build of Dahomey.Json (they fail without TWAE too; the test project doesn't target netstandard2.0,
so tests still run). The same 15 before and after on both engines.

Verdicts vs builds: check and check --build said Clean (exit 0); the builds have no new errors and no warnings, the tests
are unchanged, format converged in one changing run, a second check changed nothing. No false Clean, no false Not verified.

## Findings: 1

### 1. `--engine` breaks `bro preview format` and `bro format --diff` (both values)

What happened: `bro preview format --engine host`, `bro format --diff --engine host` and even
`bro preview format --engine dotnet-format` end with "The preview failed: 'bro format' failed (exit code 1)" and
dotnet-format's usage text. The copy's log (`logs/host-preview-format-copy.log`) shows why:

```
== net5.0 (2 project(s))

Unrecognized command or argument '--engine'.
Unrecognized command or argument 'host'.
```

`bro format --engine host` and `bro preview format` (no `--engine`) both work on the same tree. `bro preview init
--engine host` stops earlier with "Unknown option: --engine" (exit 1); `bro --help` documents `--engine` for neither, so
that one is at most a usage note, but `bro format --diff` is the documented preview of `bro format`, which does take
`--engine` (docs/format-engine.md).

Files/lines: none in the repository (nothing is written; the command fails).

Smallest reproduction (`.trials/trial50/spot-dahomey-engine`, any project works):

```bash
git init -q && dotnet new classlib -n Lib -o Lib
cat > Directory.Build.props <<'EOF'
<Project>
  <ItemGroup>
    <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial50" PrivateAssets="all" />
  </ItemGroup>
</Project>
EOF
git add -A && git commit -qm init
bro preview format Lib --engine host   # exit 1, "The preview failed"
bro preview format Lib                 # exit 0
bro format Lib --engine host           # exit 0
```

Why: `FormatCommand.Run` takes `--engine` out of the arguments (`TakeEngine`, src/StyleBro.Migrate/FormatCommand.cs:285),
but the preview formats its copy through `FormatCommand.RunPlain` (PreviewCommand.cs:96), which never calls `TakeEngine`,
so `--engine host` reaches `dotnet format` as an argument. (Either take the option in the preview and run with
`UseEngine`, or refuse it with a clear message like `preview init` does.)

Logs: `.trials/trial50/Dahomey.Json/logs/host-preview-format.log`, `host-format-diff.log`,
`default-preview-format-engine-dotnet.log`, `host-preview-format-copy.log`, `spot-preview-engine.log`.

## Other observations (not findings)

- init's note that `.trials/Directory.Build.props` is hidden by the new Directory.Build.props is the trial stub, not the repo.
- Host engine was faster for format (56 s vs 104 s); check and check --build took the same time on this small repository.

## Re-check (fixer's commit 1a25ead, PR #294)

Repacked from `claude/trial-50` at `1a25ead` as `0.8.0-trial50.2` (`dotnet tool update -g StyleBro.Tool --version
0.8.0-trial50.2`). Fresh clones of the untouched commit: `rdefault/`, `rhost/` (full runs), `p-host/`, `p-dotnet/`,
`d-host/` (previews on an unformatted tree after `init --write` + the package reference), `rpv/` (the preview init patch
applied). Logs: `.trials/trial50/Dahomey.Json/logs/recheck/`.

| Command | Default: verdict | exit | time | Host: verdict | exit | time |
|---|---|---|---|---|---|---|
| `bro preview init` | 92 files, 338 changes, clean after 1 run | 0 | 96 s | `--engine host`: "Unknown option" (unchanged; preview init has no engine) | 1 | 0 s |
| `bro init --write` | as before | 0 | 4 s | identical | 0 | 3 s |
| `bro format` | run 1: 92 files; run 2: 0, clean | 0 | 97 s | run 1: 92 files; run 2: 0, clean | 0 | 53 s |
| `bro check` (+ `--json`) | Clean | 0 | 21 s | Clean | 0 | 19 s |
| `bro check --build` (+ `--json`) | Clean (15 untouched errors not counted) | 0 | 52 s | Clean, same lines | 0 | 52 s |
| `bro preview format --engine dotnet-format` | 92 files, 338 changes, clean after 1 run | 0 | 84 s | | | |
| `bro preview format --engine host` | | | | 92 files, 338 changes, clean after 1 run | 0 | 50 s |
| `bro format --diff --engine host` | | | | 92 files, 338 changes, clean after 1 run | 0 | 48 s |

- **Finding 1 is fixed.** All three previews exit 0. Their patches (3,153 lines each) are byte-identical, and each one, applied
  to its clone, gives exactly the `rdefault` format tree (`diff -rq`). The spot repro (`bro preview format Lib --engine
  host`) now exits 0 too.
- Trees: `rdefault` == `rhost`, and both equal the first run's tree (apart from the package version in Directory.Build.props).
  The preview init patch differs from the first run's only in that version, and applied it gives the format tree.
- `--json` of check and check --build: equal apart from `path`; clean true, exit 0, one `notChecked` (IDE0049). No host
  load note.
- TWAE builds (both clones, Debug and Release): 0 warnings, the same 15 netstandard2.0 CS0122 as the untouched code. Tests:
  360 / 372 / 374 passed, 0 failed, as on the untouched code.
- Verdicts match: no false Clean or Not verified, one changing format run, no test regressions.

### New (pre-existing, not from the fix): 2. A preview of a subfolder adds a second Directory.Build.props

Seen in the spot repro, where the root `Directory.Build.props` already references StyleBro.Analyzers and the project is
in `Lib/`. `bro preview format Lib` (with or without `--engine`, so it predates PR #294; the first run never got that far
with `--engine`) prints "StyleBro.Analyzers wasn't referenced: added StyleBro.Analyzers 0.8.0-trial50.2 to
Directory.Build.props for the preview", and the patch creates `Lib/Directory.Build.props`:

```diff
+++ b/Lib/Directory.Build.props
@@ -0,0 +1,5 @@
+<Project>
+  <ItemGroup>
+    <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial50.2" PrivateAssets="all" />
+  </ItemGroup>
+</Project>
```

If someone applies the patch, that file is the nearest Directory.Build.props for `Lib/` and hides the root one: its
`<StyleBroPreset>` and anything else in the root file stop applying to that project.

Smallest reproduction (`.trials/trial50/spot-dahomey-engine`):

```bash
git init -q && dotnet new classlib -n Lib -o Lib
printf '<Project>\n  <ItemGroup>\n    <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial50.2" PrivateAssets="all" />\n  </ItemGroup>\n</Project>\n' > Directory.Build.props
git add -A && git commit -qm init
bro preview format Lib     # exit 0; "wasn't referenced"; the patch adds Lib/Directory.Build.props
```

Why (from reading the code): `PreviewCommand.Run` calls `AddPackage(target folder)` (src/StyleBro.Migrate/PreviewCommand.cs, around line 281), which
checks `ReferencesStyleBro` on the files under `Lib/` only. It doesn't look at the Directory.Build.props/.targets files
above the target, up to the repository root, that MSBuild imports. It didn't affect Dahomey.Json: its only solution is
found from the root, and the package goes into the root Directory.Build.props as in the getting-started guide.

Logs: `logs/recheck/spot-preview-noengine.log`, `spot-preview-engine.log`, `spot-preview.patch`.

### Final pass (fixer's commit 990c775)

Repacked from `claude/trial-50` at `990c775` as `0.8.0-trial50.3`. Fresh clones of the untouched commit: `r2default/` and
`r2host/` for the full runs; `p2-host/`, `p2-dotnet/` and `d2-host/` for the previews on an unformatted tree after
`init --write` and the package reference. Script: `.trials/trial50/Dahomey.Json/r2.sh`; logs: `logs/recheck2/`
(`summary.txt` has every command, exit code and time).

| Command | Default: verdict | exit | time | Host: verdict | exit | time |
|---|---|---|---|---|---|---|
| `bro preview init` (`--engine host`) | 92 files, 338 changes, clean after 1 run | 0 | 92 s | same; patch byte-identical | 0 | 48 s |
| `bro init --write` | as before | 0 | 3 s | identical | 0 | 3 s |
| `bro format` | run 1: 92 files; run 2: 0, clean | 0 | 92 s | the same | 0 | 51 s |
| `bro check` (+ `--json`) | Clean | 0 | 20 s | Clean | 0 | 19 s |
| `bro check --build` (+ `--json`) | Clean (15 untouched errors not counted) | 0 | 51 s | Clean, same lines | 0 | 49 s |
| `bro preview format --engine dotnet-format` | 92 files, 338 changes | 0 | 80 s | | | |
| `bro preview format --engine host` | | | | 92 files, 338 changes | 0 | 47 s |
| `bro format --diff --engine host` | | | | 92 files, 338 changes | 0 | 47 s |

- **Finding 2 is fixed.** The spot repro `bro preview format Lib` (with and without `--engine host`) exits 0. It doesn't
  mention adding the reference, and its patch is empty (no `Lib/Directory.Build.props`). Logs:
  `logs/recheck2/spot-preview*.log` and `.patch`.
- **Finding 1 is still fixed.** `bro preview init --engine host` is now accepted too, and its patch is byte-identical to the
  default engine's.
- Trees: `r2default` == `r2host` == the first re-check's tree (`diff -rq`). The preview init patch and the three
  format previews (3,153 lines, byte-identical), each applied to its clone, give exactly the format tree.
- `--json` of check and check --build: equal apart from `path`; clean true, exit 0, one `notChecked` (IDE0049). No host
  load note and no fallback to dotnet format.
- TWAE builds (both engines, Debug and Release): 0 warnings, the same 15 netstandard2.0 CS0122 errors as the untouched code. Tests
  360 / 372 / 374 passed, 0 failed.
- Verdicts match the builds and tests. **No findings left.**
