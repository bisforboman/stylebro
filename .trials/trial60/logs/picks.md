# Trial 60 picks (2026-10-29)

StyleBro main f85bb6c (after #312, #313, #314, #315, #316). Variant: three random plus one targeted. Trial feed
0.8.0-trial60.1 (`/home/user/feed60`, tool in `/home/user/bro60`). SDK 10.0.113 + 8.0.132.

## Random (scripts/trials/random.json, 95 entries)

`rng = random.Random(60)`, `rng.randrange(95)` three times: **39, 36, 73**. Entries 0-75, 78, 79 and 83 are in `used.tsv`.

| Draw | Entry | Result |
| --- | --- | --- |
| 39 | 39 mayuki/Cocona | used; 40-75 used; 76 SteveDunn/Vogen (702 .cs files) and 77 UglyToad/PdfPig (1,238) too large (as in trial 59); 78, 79 used; 80 apache/logging-log4net unused, 402 files: **picked** (key L4N) |
| 36 | 36 benjamin-hodgson/Pidgin | used; ... 80 picked by draw 39; 81 Azure/DotNetty 898 .cs files: too large; 82 Cysharp/MasterMemory unused, 79 files: **picked** (key MM) |
| 73 | 73 schotime/NPoco | used; 74-79 as above; 80, 82 picked; 81 too large; 83 used; 84 dotnetcore/EasyCaching unused, 310 files: **picked** (key EC) |

| Key | Repository | Commit | C# files | Setup |
| --- | --- | --- | --- | --- |
| L4N | apache/logging-log4net | 86c64ae53c0dde7650f461466cf41296c7b60217 | 402 | `src/log4net.sln`; net462;netstandard2.0 library (net462;net10.0 tests, net10.0 only off Windows), EnforceCodeStyleInBuild, AnalysisLevel 8, `WarningsAsErrors` nullable, its own 433-line `log4net.globalconfig` (GlobalAnalyzerConfigFiles), 216-line .editorconfig, tests link files from the library (`Compile Include="..\log4net\..."`), NUnit; no root Directory.Build.props (`src/` and `examples/` have their own) |
| MM | Cysharp/MasterMemory | 78e151315d9eb849ca10dc0aea4d6b866b735da4 | 79 | `MasterMemory.sln`; netstandard2.0 + an in-solution source generator, net9.0 tests (rolled forward to 10), `charset = utf-8-bom` for C#, xunit |
| EC | dotnetcore/EasyCaching | 0ce6bd0628e431463aa22d0fe8f503e2cfa32346 | 310 | `EasyCaching.sln` (40 projects); netstandard2.0;net8.0, no Directory.Build.props, no .editorconfig, a `Build.csproj` (netcoreapp3.1, FlubuCore) at the repository root, in the solution; csproj `Import`s of `build/version.props`; tests offline only without Redis/Memcached/Hybrid/ProviderFactory (servers): 892 of 897, one FasterKv big-data-set test fails untouched |

## Targeted (trial 59's fixes: tags in the preview's copy; the reference shadowed by a nested Directory.Build.props)

No unused pool entry fits (Xunit.Combinatorial used in trial 37; vs-mef and Nerdbank.GitVersioning fail to restore).
Scanned (shallow clones; .cs files, version tool, Directory.Build.props): adamralph/simple-exec 19 (MinVer; nested props
all import the root), khellang/Middleware 57 (no version tool; `src/` and `test/` props, no root, neither imports),
jbogard/Respawn 37 (MinVer in a csproj; one props), adamralph/xbehave.net 64 (archived), NimaAra/Easy.MessageHub 10,
xunit/assert.xunit 100 (none); minver, Scrutor, spectre.console, GuardClauses, Specification, SmartEnum, AutoMapper,
FakeItEasy, Polly, Particular.Approvals, lamar, Humanizer, refit, System.IO.Abstractions used.

| Key | Repository | Commit | C# files | Why / setup |
| --- | --- | --- | --- | --- |
| MW | khellang/Middleware | f31305e1dcfe865501a3daa3d07d037063ddcb7c | 57 (+ submodule) | Nested props without a root one: `src/Directory.Build.props` and `test/Directory.Build.props`, neither imports a parent (the getting-started root props is shadowed for both, `samples/` gets it). ProblemDetails compiles files from the `lib/AspNetCore` submodule (dotnet/aspnetcore at 718171e, `Compile Include="../../lib/AspNetCore/src/Shared/..."`), strong naming (signing.snk), net8.0 + netstandard2.0. NuGet.config `<clear/>`s and lists two dead dotnetfeed.blob.core.windows.net feeds (removed locally: NU1301). xunit 2 |

## Smoke (logs/smoke/)

MW: restore fails on the dead feeds (NU1301); with them removed builds (see the chain's base). MM: builds, 28 + 7 tests
pass. L4N: builds, 447 + 52 tests pass (net10.0). EC: builds; the full test run hangs on Redis/Memcached connections
(stopped), the filtered run 892 passed, 1 failed (FasterKv big data set), 4 skipped.

## MW, naive step 1 (logs in MW/logs/naive-setup-format.log)

The reference in a new root Directory.Build.props as getting-started says: init said nothing about the shadowing (it
named src/ and test/ props for EnforceCodeStyleInBuild); format named both files ("put the reference in
src/Directory.Build.props (or import the parent's there)", same for test/), fixed the rest (samples) and exited 0. The
chain follows the message: the reference in the root, src/ and test/ props.
