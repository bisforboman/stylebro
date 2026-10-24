# Trial 52 picks (2026-10-24)

## Random (seed 52)

- Rule: `.trials/README.md`'s "three random plus one targeted" variant, ONE generator: `rng = random.Random(52)`,
  `rng.randrange(50)` three times (50 entries before appending): **17, 3, 46** (0-based). (The brief also offered
  `random.Random(52 + i)`; the README's rule was followed, as in trial 51.)
- Only entry 48 (Tyrrrz/Onova) was unused; everything else in 0-49 is in `used.tsv`.
  - **Tyrrrz/Onova** (entry 48) fails the practical criteria: its tests (`Onova.Tests`) and updater target
    `net10.0-windows` / `net35` (Windows-only tests). Rejected, added to `used.tsv`.
- The list ran out, so five candidates were appended at the end (entries 50-54, alphabetical, each smoke-built and tested
  first on SDK 10.0.113 with `NuGetAudit=false`, `NUGET_CERT_REVOCATION_MODE=offline`, `DOTNET_ROLL_FORWARD=Major`):
  50 amantinband/error-or, 51 DbUp/DbUp, 52 drewnoakes/figgle, 53 EasyNetQ/EasyNetQ, 54 shibayan/Sharprompt.
  Checked and not appended: devlooped/moq (restore needs the kzu.blob.core.windows.net feed, 403 through the proxy),
  khalidabuhakmeh/ConsoleTables (3 .cs files), domaindrivendev/Swashbuckle.AspNetCore (715 .cs files).
- Walks (new length 55, walking on from the draws made with the old length):
  - Draw 17: 17-47 used, 48 Onova rejected, 49 used, **50 amantinband/error-or**.
  - Draw 3: 3-47 used, 48 rejected, 49 used, 50 picked, **51 DbUp/DbUp**.
  - Draw 46: 46-47 used, 48 rejected, 49 used, 50-51 picked, **52 drewnoakes/figgle**.
- Smoke results:
  - error-or: 48 .cs, StyleCop.Analyzers 1.2.0-beta.556 (root Directory.Build.props, central package management,
    `src/Stylecop.json`), TWAE, library netstandard2.0;net8.0;net10.0, tests net462;net8.0;net10.0 (net462 is a framework
    the library lacks; it needs Mono here, so that run fails before and after), 228 tests per framework; 42 files committed
    with CRLF and 28 with LF, no .gitattributes.
  - DbUp: 120 .cs, `src/DbUp.sln`, `src/Directory.Build.props` (TWAE) and none at the root, netstandard2.0 core, net8.0
    tests (100, Verify snapshot tests).
  - figgle: 33 .cs, `src/` and `tests/Directory.Build.props` (none at the root), central package management at the root,
    an in-solution font source generator, three test projects (net9.0: 323 + 30 + 4), a Blazor WebAssembly site and samples
    outside the props.

## Targeted

Lenses (trial 51's fixes): a multi-targeted project whose tests target a framework a referenced project lacks; several test
projects (the per-run solution filter); CRLF-committed files in an LF repository; a src/ Directory.Build.props a project
preview could shadow. No unused pool entry builds (all are `fails` or used).

- **EasyNetQ/EasyNetQ** (random.json entry 53, appended this trial, not drawn): `Source/Directory.Build.props` and
  `Source/Directory.Packages.props` (none at the root), five test projects (Tests 443, Hosepipe.Tests 7/17 pass untouched,
  Serialization.Tests 14, ApprovalTests 3; IntegrationTests need RabbitMQ in Docker, left out), EasyNetQ multi-targets
  netstandard2.0;net8.0;net9.0;net10.0 and the tests are net8.0, examples and benchmarks; 432 .cs. Added to the pool.
  (error-or, a random pick, also covers the first and third lens.)

## Commits

| Repository | Branch | Commit |
|---|---|---|
| amantinband/error-or | main | a1064860ca999a0cbb1b6fdac6d9ccfd33070e9a |
| DbUp/DbUp | main | 41228ce747979fb52e1a59e3eec5823e6de869c8 |
| drewnoakes/figgle | master | 804efc8e65276154ff7d71e2062ecd475ccd4e9f |
| EasyNetQ/EasyNetQ | master | c1dd7f2e3d34ee3b06e67a7551f7d0a9d442ff27 |
