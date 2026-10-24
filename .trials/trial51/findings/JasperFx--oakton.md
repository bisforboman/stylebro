# Trial 51: JasperFx/oakton

- Repo: https://github.com/JasperFx/oakton at `4f1ac428187dfbeb20ff0ceb2524f758300061ba` (random pick 1 of 3, seed 51, index 44).
- StyleBro: main at `73c00c5`, packed as `0.8.0-trial51` (StyleBro.Analyzers + StyleBro.Tool) into `/home/user/feed51`, the
  feed added as a global NuGet source, `dotnet tool install -g StyleBro.Tool --version 0.8.0-trial51`.
- Environment: `NUGET_CERT_REVOCATION_MODE=offline`, `NuGetAudit=false`, `DOTNET_ROLL_FORWARD=Major` (net6.0/net7.0/net9.0
  tests on the 8.0/10.0 runtimes), `CI=true`. No global.json, no nuget.config.
- Repo setup: `src/Oakton.sln`, 19 projects (library `Oakton` net6.0-net9.0, samples and demo apps net6.0-net8.0, `Tests`
  net6.0-net9.0 xUnit 2 + Shouldly), 155 .cs files, no .editorconfig, no Directory.Build.props, no analyzers of its own.
  `InternalsVisibleTo("Tests")`.

## Commands

`.trials/trial51/run.sh oakton <clone> <tag> [--engine host]` with `oakton/env.sh`; clones `base` (untouched), `default`,
`host` (all at the commit):

```
bro preview init [--engine host]
# getting-started step 1: a new Directory.Build.props with <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial51" PrivateAssets="all" />
bro init --write
bro format [--engine host]
bro check [--engine host]          (+ --json)
bro check --build [--engine host]  (+ --json)
dotnet build src/Oakton.sln -c Debug|Release --no-incremental -p:TreatWarningsAsErrors=true
dotnet test src/Tests/Tests.csproj -c Debug|Release
```

Extra: `dotnet build src/Oakton.sln -c Debug --no-incremental -p:TreatWarningsAsErrors=true -p:WarningsNotAsErrors=CS8632`
on `base` and `host` (gets past the untouched code's errors so every project compiles); the preview patch applied to a
fresh clone (`pv`) and compared with `default`.

## Baseline (untouched code)

| Build / test | Result |
|---|---|
| Debug (no TWAE) | green |
| Debug / Release TWAE | fail: 96 errors CS8632 (`?` annotations without `#nullable`, Oakton.csproj, 24 per TFM); 13 projects that need Oakton don't compile |
| TWAE with CS8632 left a warning | fails: 32 errors xUnit1013 (Tests, 4 methods x 4 TFMs x 2) |
| Tests Debug / Release | 254/255 on each of net6.0-net9.0; `CommandExecutorTester.run_an_async_command_that_fails` fails (expects `DivideByZeroException` in the output) |

## Results

| Command | Default: exit | time | Host: exit | time | Verdict (both) |
|---|---|---|---|---|---|
| `bro preview init` | 0 | 392 s | 0 | 172 s | 95 files, 447 changes, clean after 1 run; Not checked: IDE0049 (note) |
| init (step 1 + `init --write`) | 0 | 19 s | 0 | 19 s | 71 lines in .editorconfig, `StyleBroPreset` 1.0; 6 rules off (mixed), 3 off (not followed) |
| `bro format` | 0 | 321 s | 0 | 168 s | run 1: 95 files; run 2: 0, clean |
| `bro check` | 0 | 110 s | 0 | 76 s | Clean |
| `bro check --json` | 0 | | 0 | | `clean: true`, `exitCode: 0` |
| `bro check --build` | 3 | 255 s | 3 | 193 s | "Formatting is clean. But: Not verified": the 13 projects that need Oakton didn't compile in either build, "as in the untouched code's build" (48 errors the untouched code has too, not counted) |
| `bro check --build --json` | 3 | | 3 | | `clean: false`, `exitCode: 3` (`notChecked` holds only the IDE0049 note; the build's reason is only in the text output) |
| Debug / Release TWAE | 1 / 1 | 24 / 18 s | 1 / 1 | 27 / 20 s | the same 96 CS8632 as untouched, nothing else |
| TWAE, CS8632 a warning | 1 | | 1 | | the same 32 xUnit1013 as untouched (identical error lists), no BRO/IDE errors in any project |
| Tests Debug / Release | 1 / 1 | | 1 / 1 | | 254/255 x 4 TFMs, the same one failure as untouched |

Timings were taken with two or three other repositories' jobs running (4 CPUs): compare host and default only roughly.

### Engines compared

- Trees after everything: `diff -rq --exclude=.git --exclude=bin --exclude=obj default host` is empty.
- Exit codes and verdicts: identical (table). `--json` of check and check --build identical apart from `path`.
- Logs of format, check, check --build: identical apart from the order of the `Checked <tfm> (run i of 4)` lines.
- Preview patch vs format: the patch on a fresh clone gives exactly `default`'s tree, apart from Directory.Build.props
  (the preview's carries the "raise it" comment above `StyleBroPreset`; `init --write` on an existing Directory.Build.props
  that step 1 wrote doesn't add it).

### Verdicts vs builds/tests

- `bro check` Clean is right: the TWAE builds (with CS8632 relaxed so every project compiles) have no BRO/IDE errors.
- `bro check --build` "Not verified" (exit 3) is honest: the untouched code's TWAE build fails in Oakton.csproj, so 13
  projects don't compile and StyleBro's rules don't run there; the items it names are right.
- Renames (BRO1309, BRO1303): 18 private/internal methods and private fields; public/protected members kept
  (`protected void setValue` only moved by BRO1001). Tests unchanged.

## Findings

None.

## Final re-check (StyleBro at `8768a53`'s code, `0.8.0-trial51.2`)

Both engines on fresh clones: preview 0, format 0 (clean), check 0, check --build 3 (the same honest Not verified), TWAE
Debug/Release only the 96 CS8632, tests 254/255 x 4 TFMs x 2 (the same one failure). Trees, verdicts and --json identical
on both engines; the only log difference is the default engine's "had warnings loading the workspace in its net8.0 run"
note (PR #287's documented difference).
