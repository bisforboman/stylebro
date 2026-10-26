# Trial 56 picks (2026-10-26)

StyleBro main 61b1a6e (after #306: engine report parity), feed 0.8.0-trial56.1.

## Random (seed 56)

- Rule: `.trials/README.md`'s "three random plus one targeted" variant, ONE generator: `rng = random.Random(56)`,
  `rng.randrange(79)` three times (79 entries after #306's appends): **71, 1, 60** (0-based).
- Unused before this trial: 63-78 (FusionCache, then #306's 64-78, cloned and checked but not smoke-built).
- Walks:
  - Draw 71: **NickCraver/StackExchange.Exceptional** (4fccb27, 93 .cs) skipped: its only solution includes
    `samples/Samples.MVC5` (MSBuild.SDK.SystemWeb, imports Visual Studio's WebApplication targets: MSB4019 on Linux) and
    targets net462/net472/net48, a Windows-host repository by the where-trials-run table. Next: 72
    **robinrodricks/FluentFTP**.
  - Draw 1: 1-62 used, **63 ZiggyCreatures/FusionCache**.
  - Draw 60: 60-62 used, 63 picked, **64 andrewlock/NetEscapades.EnumGenerators**.
- Smoke (restore + Debug build + tests, `smoke-*.log`):
  - FluentFTP 9fa18c5 (442 .cs, library net7.0-net9.0;net462;net472;netstandard2.0/2.1, VB examples in the solution, no
    Directory.Build.props): builds; 273 tests, 3 fail untouched.
  - FusionCache af09f81 (262 .cs, MTP runner, core netstandard2.0;net8.0;net9.0;net10.0, plugins netstandard2.0): builds;
    1,625 tests in 16 min, 4 timing-sensitive failures untouched.
  - NetEscapades.EnumGenerators d3995b8 (105 .cs, in-solution generator, TreatWarningsAsErrors, net48/net451 test targets):
    builds; every suite passes.
  - DynamicData 65016e5: builds (5,042 warnings, StyleCop/Roslynator ones among them); 2,614 pass, 17 skipped.

## Targeted

Lens (trial 55's and #306's fixes): a multi-targeted solution where a referenced project lacks one of the frameworks, so a
project is reached through two paths (#306's view fix); a repository whose only solution is one folder below the root
(trial 55's baseline location fix); a generated file whose name sits inside a longer project name (trial 55's
generated-file fix).

No unused pool entry fits (the unused ones need SDK 11 or internal feeds; DiscordChatExporter has no offline tests).
`scan2.py` (the two-path condition, solution locations, generated-looking names) over the random candidates and 12
shallow clones (svg-net/SVG, reactiveui/DynamicData, rebus-org/Rebus, AngleSharp.Css, Cysharp/R3, WebApiClient,
serilog-sinks-file, serilog-extensions-logging, azure-functions-durable-extension, odata.net, Silk.NET,
StackExchange.Metrics): the two-path condition only in FusionCache (a random pick: net10.0 projects reference
netstandard2.0-only plugins that reference the core, which has net10.0, so a net10.0 run sees the core twice) and
Silk.NET (11,680 .cs, workloads). So FusionCache covers #306's lens and the targeted pick takes the baseline lens:

- **reactiveui/DynamicData** (main 65016e5, 881 .cs, not in the pool, added): the only solution is `src/DynamicData.sln`
  (src/ also holds Directory.Build.props/.targets, Directory.Packages.props and stylecop.json: StyleCop, so `bro migrate`);
  the library targets netstandard2.0;net462;net6.0;net7.0;net8.0;net9.0;net10.0, the tests net9.0; Nerdbank.GitVersioning
  (version.json at the root, full clone).

## Commits

| Repository | Branch | Commit |
|---|---|---|
| robinrodricks/FluentFTP | master | 9fa18c533acfe53fac0222f12634fafa70879cc8 |
| ZiggyCreatures/FusionCache | main | af09f81a3ea8d7ed71183b46501946da801a2a22 |
| andrewlock/NetEscapades.EnumGenerators | main | d3995b8fa8854357c902e2d2b4495ed16c5f8546 |
| reactiveui/DynamicData | main | 65016e5ba22a7c889b0a06cc1094e389281612c3 |
