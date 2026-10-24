# Trial 51: StephenCleary/AsyncEx

- Repo: https://github.com/StephenCleary/AsyncEx at `0361015459938f2eb8f3c1ad1021d19ee01c93a4` (targeted pick: a
  `Directory.Build.props` in subfolders, `src/` and `test/`, and none at the root).
- StyleBro: main at `73c00c5` as `0.8.0-trial51`. Environment: `NUGET_CERT_REVOCATION_MODE=offline`, `NuGetAudit=false`,
  `DOTNET_ROLL_FORWARD=Major` (netcoreapp3.1 tests), `CI=true`. No global.json, no nuget.config.
- Repo setup: 6 libraries `src/Nito.AsyncEx.*` (netstandard1.3;netstandard2.0;net461, TWAE, GenerateDocumentationFile,
  Microsoft.CodeAnalysis.FxCopAnalyzers 3.3.0, from `src/Directory.Build.props`), 5 test projects `test/*`
  (`<TargetFrameworks>netcoreapp3.1</TargetFrameworks>` off Windows, xUnit 2.4, from `test/Directory.Build.props`), the
  libraries reference each other (Context, Coordination, Interop, Oop -> Tasks); 59 .cs, a root .editorconfig.

## Commands

`.trials/trial51/run.sh AsyncEx <clone> <tag> [--engine host]` with `env.sh`:

```
bro preview init [--engine host]
# step 1 (getting-started: "put the reference there (or in each such file)"): the PackageReference in src/ and test/Directory.Build.props
bro init --write
bro format [--engine host]
bro check [--engine host]          (+ --json)
bro check --build [--engine host]  (+ --json)
dotnet build Nito.AsyncEx.sln -c Debug|Release --no-incremental -p:TreatWarningsAsErrors=true
dotnet test Nito.AsyncEx.sln -c Debug|Release
```

Extra: the dev build (`dotnet src/StyleBro.Migrate/bin/Debug/net8.0/StyleBro.Migrate.dll`) on clones `diag`/`diag2`;
`dotnet format` by hand on the solution and on solution filters; `bro preview format` below the root on `base` (the lens).

## Baseline (untouched code)

| Build / test | Result |
|---|---|
| Debug, Debug / Release TWAE | green (24 NETSDK1215 warnings: netstandard1.3 out of support, not errors) |
| Tests Debug / Release | 266/266 each (Tasks 90, Coordination 132, Context 22, Interop 15, Oop 7) |

## Results

| Command | Default: exit | time | Host: exit | time | Verdict |
|---|---|---|---|---|---|
| `bro preview init` | 3 | 344 s | 0 | 76 s | default 19 files / 47 changes, "not verified" (4 test projects didn't load); host 21 / 50, clean |
| init | 0 | 20 s | 0 | 20 s | 67 lines in .editorconfig; the preview put the reference into src/ and test/Directory.Build.props, as getting-started says (lens: right) |
| `bro format` | 3 | 469 s | 0 | 83 s | default "Not verified": 4 test projects skipped; host clean |
| `bro check` | 3 | 149 s | 0 | 44 s | default Not verified, host Clean |
| `bro check --build` | 2 | 382 s | 0 | 160 s | default "Not clean": 3 findings in Coordination.UnitTests in both builds (the files format skipped); host Clean |
| TWAE Debug / Release | 1 / 1 | | 0 / 0 | | default: BRO1517 + 2 x BRO1508 in test/AsyncEx.Coordination.UnitTests (untouched: green); host green |
| Tests Debug / Release | 0 / 0 | | 0 / 0 | | 266/266 each |

The netstandard1.3 run skips the libraries ('dotnet format' finds no mscorlib for netstandard1.x) and a netstandard2.0 run
stands in for it ("netstandard2.0, instead of netstandard1.3"): by design, both engines.

### Engines compared

- Trees differ: the host formatted `AsyncLazyUnitTests.cs` and `AsyncSemaphoreUnitTests.cs` (test/AsyncEx.Coordination.UnitTests),
  the default engine skipped them. Verdicts and exit codes differ on preview, format, check, check --build (default 3/3/3/2,
  host 0). The host is right: the dev build with the fix gives exactly the host's tree (`diff -rq host diag2` empty).
- The default engine's verdicts were honest about what it did (Not verified, then the build's findings), but the skip is
  a bug (finding 1).

## Finding 1: the test projects whose library references another library don't load in the default engine (false "Not verified", then findings for the build)

Format, check and the preview: "'dotnet format' skipped AsyncEx.Context.UnitTests ... in its netcoreapp3.1 run" (and
Coordination, Interop, Oop; Tasks.UnitTests loaded), "Not checked: AsyncEx.*.UnitTests: 'dotnet format' couldn't load it
(references didn't load)". check --build then found 3 findings left in Coordination.UnitTests, and the TWAE builds that
were green untouched failed on them.

Cause: the netcoreapp3.1 run's solution filter lists only the run's projects (the 5 test projects). The libraries load as
references, outside the filter, and MSBuildWorkspace then wires Context's reference to Tasks to Tasks' first framework,
netstandard1.3. 'dotnet format' skips a project when a project it references has no mscorlib.dll reference
("Required references did not load for X or referenced project"), which netstandard1.x never has. Reproduced by hand,
without bro: `dotnet format tests-only.slnf --verify-no-changes` (the 5 test projects) skips the 4 tests; the whole
solution, or a filter that also lists the libraries (`tests-libs.slnf --include test/`), skips none.

Smallest reproduction (`.trials/trial51/spot/chain/`, own git repo): Lib1 (netstandard1.3;netstandard2.0), Lib2 (same,
references Lib1), Tests (net8.0, references Lib2) with a whitespace finding. `bro check`: "skipped Tests ... in its net8.0
run", "Not checked: Tests". The dev build with the fix: only the whitespace finding.

Fix (`b5f24c8`): `FormatCommand.FilterProjects`: a run's solution filter also lists the solution's projects its projects
reference, directly or not; `--include` still limits the run to its own projects' files (`FormatCommand.Include`). Test:
`MigrationTests.Format_ATestProjectWhoseLibrariesReferenceEachOther_Loads` (real restore and `FormatCommand.Run` on the
chain shape; fails with the call site reverted). Mutation entries `Migrate/FormatCommand-98f48a97.psd1` (call site) and
`Migrate/FormatCommand-9effb800.psd1` (the transitive add). After: `diag2` format exit 0, clean, 21 files (the host's tree).

## Lens (trial 50's subfolder fix): `bro preview format` below the root

- `bro preview format src` (exit 1): "No solution or project in .../src; name one" (src/ holds six projects and no solution):
  as documented.
- `bro preview format src/Nito.AsyncEx.Tasks/Nito.AsyncEx.Tasks.csproj` on the untouched clone (exit 0, 14 files, clean
  after 1 run): finding 2.

## Finding 2: a preview of a project creates a Directory.Build.props in its folder that hides the one above

The preview said "Settings: 5 lines in src/Nito.AsyncEx.Tasks/Directory.Build.props" and its patch adds that file (a new
`<Project>` with only the StyleBro.Analyzers reference). MSBuild imports only the nearest Directory.Build.props, so the
project would lose `src/Directory.Build.props`: TreatWarningsAsErrors, GenerateDocumentationFile, FxCopAnalyzers and
project.props (version, RootNamespace). The preview's own format ran without them, so it doesn't show what the build
reports. getting-started step 1 says to put the reference into the nearest Directory.Build.props (where `bro preview init`
on the whole repository put it: src/ and test/).

Cause: `PreviewCommand.AddPackage(root, top)` picks `Migration.PropsFiles(root)`, which looks for the nearest
Directory.Build.props only inside the previewed root (the project's folder), so it found none and named a new one there.
Trial 50 made the files above the root count when checking for an existing reference (`ImportedFromAbove`), not when
choosing where to add one; its test even asserted the new file. `CentralPackages(file, root)` had the same bound: a
version-less reference would have lost the Directory.Packages.props above.

Smallest reproduction: `src/Directory.Build.props` (any content), `src/Tasks/Tasks.csproj`; `AddPackage(src/Tasks, root)`
created `src/Tasks/Directory.Build.props`.

Fix (`3bf667a`): when the root is below the copy's top, AddPackage takes the props files from the top for the projects under
the root (`Migration.PropsFiles(top, [folder])`) and bounds `CentralPackages`/`OptOuts`/`CentralProjects` and the printed
names by the top. Test: `MigrationTests.Preview_OfAProject_AddsThePackageToTheDirectoryBuildPropsItImports` (also the
central-package case); trial 50's `Preview_OfASubfolder_SeesTheReferenceAbove` now expects the root's file to get it.
Mutation entries `Migrate/PreviewCommand-30f5f919.psd1`, `Migrate/PreviewCommand-7343c758.psd1`.

## Final re-check (StyleBro at `8768a53`'s code, `0.8.0-trial51.2`)

Both engines on fresh clones: preview 0 (21 files, 50 changes, clean after 1 run), format 0, check 0, check --build 0, TWAE
Debug/Release green, tests 266/266 x 2. Trees, verdicts, logs and --json identical on both engines.
