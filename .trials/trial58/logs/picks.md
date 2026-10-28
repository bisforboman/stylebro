# Trial 58 picks (2026-10-27)

StyleBro main 556db04 (after #310, the fixture matrix). Variant: three random plus one targeted.

## Random (scripts/trials/random.json, 79 entries)

`rng = random.Random(58)`, `rng.randrange(79)` three times: **74, 25, 26**.

| Draw | Entry | Result |
| --- | --- | --- |
| 74 | 74 seesharper/LightInject | unused: **picked** (key LightInject) |
| 25 | 25 wiremock/WireMock.Net | used (trial 36); 26 xunit/xunit rejected (trial 38); 27-69 used; 70 natemcmaster/CommandLineUtils unused: **picked** (key CLU) |
| 26 | 26 xunit/xunit | rejected (trial 38); 27-69 used; 70 picked by draw 25; 71 StackExchange.Exceptional rejected (trial 56); 72 FluentFTP used; 73 schotime/NPoco unused: **picked** (key NPoco) |

After this trial only entries 76-78 (Vogen 702 files, PdfPig 1,236 files, SharpYaml) are unused: new candidates appended.

| Key | Repository | Commit | C# files | Why / setup |
| --- | --- | --- | --- | --- |
| LightInject | seesharper/LightInject | b943de2168becd99e88f47a8b53b6ff44f85a7e4 | 102 | StyleCop 1.1.118 in the library only (migrate), net8.0/netstandard2.0/net462, WarningsAsErrors, LangVersion 8, one 8,000-line file; global.json 8.0.402 lowered; tests need the `ilverify` global tool (installed: dotnet-ilverify 10.0.14) |
| CLU | natemcmaster/CommandLineUtils | 00c331e7359cf5e6e5ace82ede6984cb14d760bd | 279 | TWAE + EnforceCodeStyleInBuild, `.editorconfig`, BaseIntermediateOutputPath `.build/obj`, in-solution generator, net8.0/net10.0/net472/netstandard2.0; net472 tests need mono (aborted untouched too) |
| NPoco | schotime/NPoco | 61ba5d16f6a9543d9cc2c5a97a63663da299b5ea | 325 | net462/netstandard2.0/2.1/net8.0/net10.0 libraries, no Directory.Build.props, NUnit; test config switched from LocalDB (2) to SQLite in-memory (1) locally |

## Targeted (#310's VB fixer fix: C# projects referencing a Visual Basic project)

No pool entry fits (no `visual-basic` tag; the git-height pick DapperAOT is used, Nerdbank.GitVersioning fails).
Scanned (blobless clones, `.vbproj` count): CodeConverter 14 (VSIX, test data), ReadableExpressions 1 (net35/netstandard1.3 VB
project, Windows visualizers), AgileMapper 1 (1,047 files), NSubstitute.Analyzers 1 (benchmark source only), odata.net 2
(5,955 files), ExpressionTreeToString 1; none in impromptu-interface, dynamitey, LINQKit, xunit.analyzers, samples.xunit,
ApprovalTests.Net, PublicApiGenerator, Ninject, Windsor, Humanizer.

| Key | Repository | Commit | C# files | Why / setup |
| --- | --- | --- | --- | --- |
| ETTS | zspitz/ExpressionTreeToString | ca6e099c1f92a1dbe152d95484da75f6eafd157b | 92 | C# tests (net452-net6.0) referencing `TestObjects.VB.vbproj` (13 .vb files) next to a C# twin: the shape where the host's IDE0036 VB fixer deleted a method; tests on net6.0 (net4x needs mono); 9 of 3,089 fail untouched |

## Smoke (Debug build + tests, untouched)

LightInject build ok, 1,568 tests (261 failed only for the missing ilverify tool); CLU build ok, 1,143 pass (net472 aborted:
mono); NPoco build ok, 367 pass, the database fixtures need LocalDB (SQLite in-memory instead); ETTS build ok, 3,080/3,089.
