# Trial 53 picks (2026-10-24)

## Random (seed 53)

- Rule: `.trials/README.md`'s "three random plus one targeted" variant, ONE generator: `rng = random.Random(53)`,
  `rng.randrange(55)` three times (55 entries after trial 52's appends): **39, 54, 13** (0-based).
- Unused before appending: only index 54 (shibayan/Sharprompt, appended and smoke-built by trial 52). Every other index in
  0-53 is in `used.tsv` (48 Tyrrrz/Onova as rejected).
- The list ran out, so five candidates were appended at the end (indices 55-59 = entries 56-60, alphabetical ignoring
  case), each smoke-built and tested first on SDK 10.0.113 + 8.0.132 (`NuGetAudit=false`, `NUGET_CERT_REVOCATION_MODE=offline`,
  `DOTNET_ROLL_FORWARD=Major`):
  - 55 AArnott/Nerdbank.Streams (main e1743fd5): 132 .cs, StyleCop.Analyzers (root Directory.Build.props, central package
    management), global.json 10.0.402 (lowered locally), needs `git submodule update --init` (ext/MessagePack sources are
    compiled in); MTP tests: all pass.
  - 56 autofac/Autofac.Extensions.DependencyInjection (develop 206de12c): 72 .cs, StyleCop.Analyzers per project, `.editorconfig`
    `end_of_line = crlf` while all 72 C# files are committed LF (no `text` attribute in .gitattributes), global.json 10.0.203
    (lowered locally); tests 571 + 559 + 2 + 1 + 1, all pass.
  - 57 emgarten/Sleet (main 833134d1): 228 .cs, global.json 10.0.401 (lowered locally), MTP: 1,881 pass, 1 fails untouched
    (Sleet.CliTool.Tests), 43 skipped.
  - 58 Finbuckle/Finbuckle.MultiTenant (main 3765bade): 173 .cs, net10.0, 607 tests pass.
  - 59 tmenier/Flurl (dev 36a3fa2b): 81 .cs, 521/547 tests pass offline (26 `RealHttpTests` need the network).
  Checked and not appended (used, too small or too large): thomaslevesque/WeakEvent (11 .cs), louthy/language-ext (1,756),
  cake-build/cake (1,893), GitTools/GitVersion (893), Azure/DotNetty (898); many others were in `used.tsv` already.
- Walks (new length 60, walking on from the draws made with the old length):
  - Draw 39: 39-53 used, **54 shibayan/Sharprompt**.
  - Draw 54: 54 picked, **55 AArnott/Nerdbank.Streams**.
  - Draw 13: 13-53 used, 54-55 picked, **56 autofac/Autofac.Extensions.DependencyInjection**.

## Targeted

Lenses (trial 52's fixes): a `.nupkg` committed inside the source tree (not an extracted package folder); StyleCop with an
`end_of_line` some committed files don't follow (migrate's new minority note). No unused pool entry has either.

- **loic-sharma/BaGet** (main 5a8502ca, 304 .cs, not in the pool, added): `tests/BaGet.Tests/TestData/` holds
  `TestData.1.2.3.nupkg` and `.snupkg` next to `TestData.Designer.cs` and `TestData.resx`, and no project: the folder is NOT
  an extracted package but trial 52's `IsExtractedPackage` (no project/solution/Directory.Build.* there) still says it is.
  netcoreapp3.1 (DOTNET_ROLL_FORWARD=Major) + netstandard2.0, `.editorconfig` end_of_line = lf, no analyzers. Tests
  untouched: Core 117, Protocol 46, Web 20 pass; BaGet.Tests 47 of 66 fail (FormatException "The format of value '' is
  invalid"), Protocol.Samples.Tests 2 of 8 fail (network: nuget.org samples).
- The line-ending lens: Autofac.Extensions.DependencyInjection (random) has StyleCop with `end_of_line = crlf` and every file
  LF (the contradicted case, not the minority one). Checked for the minority case and not taken: microsoft/durabletask-dotnet
  (StyleCop, `end_of_line = crlf`, 522 LF / 16 CRLF / 16 mixed: also the contradicted case, 555 .cs, Grpc emulator tests).

## Commits

| Repository | Branch | Commit |
|---|---|---|
| shibayan/Sharprompt | master | 5e17ace494e7e7a87cd0daa3e3481eead749a68f |
| AArnott/Nerdbank.Streams | main | e1743fd5d41733caa3e7a9b62a55f4226fa79a94 |
| autofac/Autofac.Extensions.DependencyInjection | develop | 206de12cc40baa58cc6f807e005f99d2e44c1ae4 |
| loic-sharma/BaGet | main | 5a8502ca4e3b21acd369d4a98c2529dac2d81cfe |
