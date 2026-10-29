# Trial 61 picks (2026-10-29)

Variant: three random plus one targeted. SDK 10.0.113 + 8.0.132. Tested: main after #318 (trial 60's fixes).

## Random (scripts/trials/random.json, 95 entries)

`rng = random.Random(61)`, `rng.randrange(95)` three times: **63, 23, 71**. Entries 0-75, 78-80, 82-84 are in `used.tsv`
(80, 82, 84 since trial 60), 76 Vogen (702 .cs files), 77 PdfPig (1,238) and 81 DotNetty (898) too large.

| Draw | Entry | Result |
| --- | --- | --- |
| 63 | 63 | used; 64-75 used; 76, 77 too large; 78-80 used; 81 too large; 82-84 used; 85 fluentscheduler/FluentScheduler unused, 39 files: **picked** (key FS) |
| 23 | 23 | used; 24-84 as above; 85 picked by draw 63; 86 jbogard/Respawn unused, 37 files: **picked** (key RS) |
| 71 | 71 | used; 72-84 as above; 85, 86 picked; 87 mganss/ExcelMapper unused, 20 files: **picked** (key EM) |

| Key | Repository | Commit | C# files | Setup |
| --- | --- | --- | --- | --- |
| FS | fluentscheduler/FluentScheduler | 04f64f85dd88aaa2681789167e201a8db58df716 | 39 | FluentScheduler.sln, netstandard2.0 library + a test app, .editorconfig, no Directory.Build.props, MSTest/xunit unit tests (98) |
| RS | jbogard/Respawn | f550203c2caaded99ede64d85ff07acc6bb7514d | 37 | Respawn.sln, netstandard2.0..net10.0 library, TreatWarningsAsErrors + LangVersion 12 in Directory.Build.props, global.json 10.0.100 latestFeature; Respawn.DatabaseTests need SQL Server/Postgres/MySQL/Oracle/Informix (left out), Respawn.UnitTests 14 |
| EM | mganss/ExcelMapper | 035a2bc3acd6d1046a0bb244e85ef579c7116724 | 20 | ExcelMapper.sln, net462/netstandard2.0 library, NUnit; no .editorconfig or props; SaveTest fails untouched (122 tests) |

## Targeted (trial 60's fixes: BRO1120 leaves `//` file headers with empty comment lines alone; submodules)

No unused pool entry fits. jbevain/cecil: every file starts with a Mono-style header whose first and last lines are an
empty `//` (exactly what trial 60's BRO1120 removed in log4net), unused, 243 files.

| Key | Repository | Commit | C# files | Why / setup |
| --- | --- | --- | --- | --- |
| CE | jbevain/cecil | 882ca5eedda1e62eb41bd5869aeb15d8f1538e51 | 243 | `//\n// Author: ...\n//\n// Licensed under the MIT/X11 license.\n//` headers; Mono.Cecil.sln, netstandard2.0;net40 (+ net8.0 tests), EnableDefaultItems false in Directory.Build.props (explicit Compile items), PublicSign off Windows, NUnit; tests net8.0 only (net40 needs mono); 19 of 359 Mono.Cecil.Tests fail untouched (no ilasm on Linux) |

## Smoke (logs/smoke/)

All four restore and build. Tests: FS 98 pass; RS UnitTests 14 pass; EM 121 + 1 failing (SaveTest); CE 15 + 43 + 4 pass,
Mono.Cecil.Tests 335 pass, 19 fail (ilasm), 5 skipped.
