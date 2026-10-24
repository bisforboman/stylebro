# Trial 51: mgravell/Pipelines.Sockets.Unofficial

- Repo: https://github.com/mgravell/Pipelines.Sockets.Unofficial at `242baeee3b627ae4bef295a958f7202c6bf66da3` (random pick 2
  of 3, seed 51, index 45), full clone (Nerdbank.GitVersioning).
- Local workaround (every clone, committed with the noreply identity): `git mv Directory.build.props Directory.Build.props`.
  Linux is case-sensitive and MSBuild looks for `Directory.Build.props`, so untouched the file isn't imported here: the
  net462/net472 builds get C# 7.3 and fail (CS8370). Windows imports it as is.
- StyleBro: main at `73c00c5` as `0.8.0-trial51` (feed `/home/user/feed51`, tool installed with `dotnet tool install -g`).
- Environment: `NUGET_CERT_REVOCATION_MODE=offline`, `NuGetAudit=false`, `DOTNET_ROLL_FORWARD=Major`, `CI=true`. global.json
  10.0.102 with `latestMajor` (10.0.113 used). NuGet.config `<clear/>`s to nuget.org; the trial feed is a global source
  and StyleBro.Analyzers came from the global packages folder (restored there by an earlier repository's run).
- Repo setup: library (net462;net472;netstandard2.0;netstandard2.1;net8.0;net10.0), tests (net472;net8.0;net10.0, xUnit 2,
  compiling `..\Benchmark\*Benchmarks.cs`), Benchmark (net472;net8.0;net10.0), Toys (exe, net8.0;net462;net10.0),
  AotTests (net8.0); 77 .cs, no .editorconfig, `Shared.ruleset`, no analyzers of its own.

## Commands

`.trials/trial51/run.sh Pipelines.Sockets.Unofficial <clone> <tag> [--engine host]` with `env.sh`:

```
bro preview init [--engine host]
# step 1: <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial51" PrivateAssets="all" /> next to SourceLink in Directory.Build.props
bro init --write
bro format [--engine host]
bro check [--engine host]          (+ --json)
bro check --build [--engine host]  (+ --json)
dotnet build Pipelines.Sockets.Unofficial.sln -c Debug|Release --no-incremental -p:TreatWarningsAsErrors=true
dotnet test tests/Pipelines.Sockets.Unofficial.Tests -c Debug|Release -f net8.0   (and -f net10.0; net472 needs Mono)
```

## Baseline (untouched code, with the rename)

| Build / test | Result |
|---|---|
| Debug | green |
| Debug / Release TWAE | fail: 4 errors CS0649 (library, net8.0 and net10.0); AotTests doesn't compile |
| Tests Debug | 286/292 on net8.0 and net10.0, 5 skipped, `BufferWriterDoesNotLeak` fails (GC-dependent; passes in some runs) |
| Tests Release | 287/292 net8.0, 286/292 net10.0 (`BufferWriterDoesNotLeak`) |

## Results

| Command | Default: exit | time | Host: exit | time | Verdict |
|---|---|---|---|---|---|
| `bro preview init` | 3 | 551 s | 0 | 242 s | both 58 files, 472 changes; default "not verified": Tests (net472) didn't load; host clean |
| init | 0 | 11 s | 0 | 10 s | 89 lines in .editorconfig |
| `bro format` | 3 | 816 s | 0 | 214 s | both: run 1 changed 53+ files, run 2 none; default "Not verified" (Tests net472), host clean |
| `bro check` | 3 | 124 s | 0 | 148 s | default "Not verified" (finding 1), host Clean |
| `bro check --json` | 3 | | 0 | | default `clean: false` + the Tests (net472) item; host `clean: true` |
| `bro check --build` | 3 | 301 s | 3 | 409 s | both "Not verified": AotTests didn't compile, as in the untouched build (honest) |
| TWAE Debug / Release | 1 / 1 | | 1 / 1 | | the same 4 CS0649 as untouched, nothing else; the TWAE build compiled the net472 tests without findings |
| Tests Debug | 1 | | 1 | | default: net8.0 287/292, net10.0 `BufferWriterDoesNotLeak`; host: `BufferWriterDoesNotLeak` + `MutexSlim_ConcurrentLoadAsync_DisableContext` ("expected 25 but was 21", a lock benchmark under load; the trees are byte-identical) |
| Tests Release | 0 | | 0 | | 287/292 x 2 |

12 runs per format (Release configurations too). Timings with other repositories' jobs running (load 20-35 on 4 CPUs).

### Engines compared

- Trees: identical (`diff -rq` empty). Exit codes and verdicts differ on preview, format and check: default 3 (Not verified,
  finding 1), host 0. `--json` the same way. check --build agrees (3, AotTests).
- That difference is the default engine's bug (finding 1), not the host's: the host loads every framework itself.

## Finding 1: the net472 tests don't load in the default engine's net472 run (false "Not verified")

`bro format`, `bro check` and the preview: "Not checked: Pipelines.Sockets.Unofficial.Tests (net472): 'dotnet format'
couldn't load it (references didn't load)". The build compiles those tests fine (the TWAE builds compiled the net472 tests
without findings), and the host engine loads them.

Cause: `FormatCommand.Keep` puts a project on the run's keep list when it targets the run's framework or references a
kept project. Toys (net8.0;net462;net10.0) references the library, so the net472 run kept it, and `SelectFrameworkTargets`
clears the environment's TargetFramework only for projects off the keep list: Toys was forced onto net472, which it doesn't
target. MSBuild (`--verbosity diagnostic`): "Msbuild failed when processing the file '.../Toys/Toys.csproj' with message:
The reference assemblies for .NETFramework,Version=v4.7.2 were not found", and the tests that reference Toys were skipped.
Toys is also in `FormatCommand.Other` (no net472, references a kept project), whose references already ask for all of
the referenced projects' frameworks (`StyleBroFormatLoose`).

Smallest reproduction (`.trials/trial51/spot/keep-other/`, own git repo): Lib (net462;net472;net8.0), Toys (exe,
net8.0;net462) referencing Lib, Tests (net472;net8.0) referencing both, `#if NET472` code in Tests with a whitespace
finding. `bro check`: "skipped Tests ... in its net472 run", "Not checked: Tests (net472)"; `dotnet build` green.

Fix (`e98542c`): `SelectFrameworkTargets` also clears TargetFramework for the projects in `StyleBroFormatOther` (they lack the
run's framework by definition). Test: `MigrationTests.Format_AMultiTargetedProjectWithoutTheRunsFramework_LoadsItsOwnFrameworks`
(builds the shape with the run's environment: NETSDK1005 before the fix, green after). Mutation entry
`Migrate/FormatCommand-37c58770.psd1`.

## Side check: the untouched clone (lower-case `Directory.build.props`) on Linux

`bro preview init --keep` on an untouched clone (exit 3): the same 58 files and the same net472 "Not verified" (finding 1).
The preview created a new `Directory.Build.props` (9 lines) next to `Directory.build.props`: on Linux that is the file
MSBuild imports, so the reference reaches every project there (the repository's own settings never do on Linux, the reason
for the workaround). On Windows both names are one file; a patch made on Linux wouldn't apply there. Not a finding: the
untouched repository doesn't build on Linux.

## Final re-check (StyleBro at `8768a53`'s code, `0.8.0-trial51.2`)

Fresh clones after the container restart, both engines. Default: format 0 (clean, finding 1 gone), check 0, check --build
3 (AotTests, honest, as before), TWAE Debug/Release the same CS0649 as untouched (4 per configuration), tests net8.0/net10.0
287/292 apart from one `BufferWriterDoesNotLeak` failure in Debug (fails untouched too). Host: the same (its
`BufferWriterDoesNotLeak` failure on net10.0 Debug). Trees, exit codes, verdicts and --json identical. Log differences: the
workspace-load note names net472 (default) or net462 (host), PR #287's documented difference; the net472 fix run's count
says 51 (default) or 52 (host) files with findings, because the engines ran net462 and net472 in another order and a run
also formats what an earlier run of the round changed (the trees are identical).

The default engine's first preview on the fresh container failed (exit 1, NU1102): the repository's NuGet.config
`<clear/>`s every source, so the restore saw no trial feed, and StyleBro.Analyzers wasn't in the global packages folder yet
(in the first runs an earlier repository had restored it there). StyleBro's message named the cause and the fix (add the
feed to nuget.config). Re-run once the package was there (as in the first runs): exit 0, 58 files, 472 changes, clean after
1 run, the same as the host's preview. An environment effect, not a finding.
