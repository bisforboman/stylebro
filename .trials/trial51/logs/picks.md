# Trial 51 picks (2026-10-24)

## Random (seed 51)

- `scripts/trials/random.json` has 50 entries (nothing appended before drawing).
- `rng = random.Random(51)`; `rng.randrange(50)` three times: **15, 32, 35** (0-based indices).
  - Draw 15: indices 15-43 used or rejected (`used.tsv`); walks to index 44 **JasperFx/oakton**.
  - Draw 32: indices 32-43 used or rejected, 44 already picked; walks to index 45 **mgravell/Pipelines.Sockets.Unofficial**.
  - Draw 35: indices 35-43 used or rejected, 44-45 picked, 46 ncalc/ncalc used (trial 50); walks to index 47
    **sebastienros/esprima-dotnet**.
- Smoke check (SDK 10.0.113, `DOTNET_ROLL_FORWARD=Major`, `NuGetAudit=false`, `NUGET_CERT_REVOCATION_MODE=offline`):
  - oakton: `src/Oakton.sln` builds; `dotnet test src/Tests` 254/255 on net6.0-net9.0 (`run_an_async_command_that_fails` fails
    untouched: expects `DivideByZeroException` in the output). 155 .cs, net6.0-net9.0, no .editorconfig, no Directory.Build.props.
  - Pipelines.Sockets.Unofficial: its `Directory.build.props` (lower-case b) isn't imported on Linux (case-sensitive), so the
    net462/net472 builds get C# 7.3 and fail (CS8370). Local workaround in every clone: `git mv Directory.build.props
    Directory.Build.props`, committed (what Windows sees). Then it builds; tests net8.0 + net10.0 286/292 passed, 5 skipped,
    `BufferWriterDoesNotLeak` fails untouched; net472 needs Mono. Full clone (Nerdbank.GitVersioning). 77 .cs.
  - esprima-dotnet: Esprima.Tests.Test262 runs `dotnet test262 generate` before its build, which downloads the test262 suite
    and exits 255 here: that project fails the same way before and after, and its tests are left out. Esprima.Tests 7,880 +
    Esprima.Tests.SourceGenerators 2 pass (net8.0; net462 needs Mono). 249 .cs, TWAE, central package management, a dense
    .editorconfig, an in-solution source generator, `test/Esprima.Tests/Fixtures.RegExp/Generator/Directory.Build.props` (a
    project outside the solution).

## Targeted

Lenses (trial 50's fixes): a generator's AdditionalFiles that another project compiles; a test project with a method named
like an entry point; a `Directory.Build.props` in a subfolder. No unused pool entry fits. Probed shallow clones (cs files,
subfolder props, `static ... Main(` in test folders): Cysharp/MessagePipe (551 .cs), Cysharp/SimdLinq, tmenier/Flurl,
xoofx/SharpYaml, benaadams/Ben.Demystifier, Open-NET-Libraries/Open.ChannelExtensions (its test project's
`public static void Main() {}` sits under `#if NET472` in a net9.0-only project: never compiled, a weak lens),
andrewlock/NetEscapades.EnumGenerators, JasperFx/lamar (691 .cs), silkfire/Pastel (6 .cs), seesharper/LightInject (no
subfolder props).

- **StephenCleary/AsyncEx**: `src/Directory.Build.props` and `test/Directory.Build.props`, none at the root (getting-started
  step 1's "nearest Directory.Build.props" case; where `bro preview init` puts the reference), TWAE + FxCopAnalyzers 3.3.0
  in src/, netstandard1.3;netstandard2.0;net461 libraries, netcoreapp3.1 xunit tests (net461 only on Windows), 59 .cs.
  Smoke: Debug build green, 266/266 tests (netcoreapp3.1, `DOTNET_ROLL_FORWARD=Major`).

## Commits

| Repository | Branch | Commit |
|---|---|---|
| JasperFx/oakton | master | 4f1ac428187dfbeb20ff0ceb2524f758300061ba |
| mgravell/Pipelines.Sockets.Unofficial | main | 242baeee3b627ae4bef295a958f7202c6bf66da3 (+ the local rename commit) |
| sebastienros/esprima-dotnet | main | 8ba15fcf9a8358c2fc7b5db8bd5e76f93a0e667c |
| StephenCleary/AsyncEx | master | 0361015459938f2eb8f3c1ad1021d19ee01c93a4 |

## Note

The session's container restarted at about 14:00 UTC (after the re-check of oakton and AsyncEx and part of the other two):
everything under `.trials/trial51/` was lost (gitignored). The findings files, this log and the scripts were recreated from
the session's record; the clones and logs of the first runs are gone (their results are in the findings files). The
re-check of esprima-dotnet and Pipelines.Sockets.Unofficial was restarted on fresh clones.
