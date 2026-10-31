# Trial 64 picks (2026-10-31)

Variant: three random plus one targeted. SDK 10.0.114 + 8.0.133. Tested: main 7a1e23a (after #321, trial 63's fixes).

## Random (scripts/trials/random.json, 104 entries when drawn)

`rng = random.Random(64)`, `rng.randrange(104)` three times: **60, 15, 80**. Unused entries: 92, 93, 98-103 (everything
else is in `used.tsv`).

| Draw | Entry | Result |
| --- | --- | --- |
| 60 | 60 | Cysharp/ZLinq used (trial 55); 61-91 used; 92 Sergio0694/PolySharp: global.json still needs SDK 11.0.100-rc.3, every project net11.0 (fails the criteria); 93 statiqdev/Statiq.Framework: too large (857 files, trial 63); 94-97 used; 98 serilog/serilog-expressions unused, 162 files: **picked** (key SE) |
| 15 | 15 | danielgerlag/workflow-core used (trial 40); 16-97 used, rejected or as above; 98 picked by the first draw; 99 serilog/serilog-extensions-logging unused, 33 files: **picked** (key SL) |
| 80 | 80 | apache/logging-log4net used (trial 60); 81-97 as above; 98, 99 picked; 100 serilog/serilog-sinks-async unused, 19 files (ExcelMapper's 20 passed in trial 61): **picked** (key SA) |

All three are serilog organization repositories (the list's tail is alphabetical); they differ in shape: SE has no
.editorconfig and net9.0 tests, SL has an .editorconfig and a net48 test target, SA has no Directory.Build.props at all.

| Key | Repository | Commit | C# files | Setup |
| --- | --- | --- | --- | --- |
| SE | serilog/serilog-expressions | 697c8fa89ec167b1f3f9457c8cee500ad02e8bc2 | 162 | Directory.Build.props (TreatWarningsAsErrors, Nullable, ImplicitUsings, docs, signing), no .editorconfig, global.json 9.0.200 (lowered); library net9.0;net8.0;net6.0;netstandard2.0 (+ net471/net462 on Windows), tests net9.0 |
| SL | serilog/serilog-extensions-logging | 8de073c001421145b6adb73d2807f274d96052d3 | 33 | the same props, .editorconfig; library net462..net10.0, tests net8.0;net9.0;net10.0;net48 (net48 left out: mono) |
| SA | serilog/serilog-sinks-async | 1ae2c3ba5809b340adac0e293494fc5c725875db | 19 | no Directory.Build.props or .editorconfig, TWAE and LangVersion 12 in the csproj files; library net8.0;net6.0;netstandard2.0 (+ net471/net462 on Windows), tests net6.0;net8.0 |

## Targeted (trial 63's fixes: BRO1126 keeps a nullable type argument's `?` in a qualified using alias, init's
`stylebro_allow_adjacent_single_line_members`, the first format of a fresh multi-targeted clone plans frameworks after the restore)

Scanned with `shape.py` (nullable using aliases, adjacent one-line member pairs, multi-targeted projects): SimonCropp/Polyfill
(6,084 files), thomhurst/TUnit (3,114 files, net11.0 workloads), Cysharp/SimdLinq, Cysharp/NativeMemoryArray,
mgravell/fast-member, kzu/ThisAssembly (too small or no shapes), TNG/ArchUnitNET; used already: Tyrrrz/PolyShim,
NSubstitute, lamar, moq, Gress, spectre.console, kiota-dotnet, CliWrap, YoutubeExplode, ZString, Ulid, Scrutor, refit,
SmartEnum, GuardClauses, CsvHelper, command-line-api, Humanizer, Bogus, Mediator. None of the scanned repositories has a
using alias with a nullable type argument (rare: that lens stays covered by the unit test).

| Key | Repository | Commit | C# files | Why / setup |
| --- | --- | --- | --- | --- |
| AU | TNG/ArchUnitNET | f439e0d2e9a45fe9f6a70fea1b05b7c1cbbf465b | 544 | 632 adjacent one-line member pairs; multi-targeted libraries (netstandard2.0;net462 / net472) on a fresh clone's first format; no Directory.Build.props or .editorconfig; global.json 10.0.400 (lowered); 25 net10.0 projects: tests for MSTest v2/v3/v4, NUnit, TUnit (MTP, its own global.json), xUnit v2/v3, plus test assemblies the tests inspect by reflection (renames there are a lens) |
