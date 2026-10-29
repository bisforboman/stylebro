# Trial 62 picks (2026-10-29)

Variant: three random plus one targeted. SDK 10.0.113 + 8.0.132. Tested: main 9d259f5 (after #319, trial 61's fixes).

## Random (scripts/trials/random.json, 95 entries)

`rng = random.Random(62)`, `rng.randrange(95)` three times: **73, 22, 8**. Entries 0-75, 78-80, 82-87 are in `used.tsv`
(85-87 since trial 61), 76 Vogen (702 .cs files), 77 PdfPig (1,238) and 81 DotNetty (898) too large (trial 61).

| Draw | Entry | Result |
| --- | --- | --- |
| 73 | 73 | used; 74, 75 used; 76, 77 too large; 78-80 used; 81 too large; 82-87 used; 88 ninject/Ninject unused, 335 files: **picked** (key NJ) |
| 22 | 22 | used; 23-87 as above; 88 picked by draw 73; 89 rebus-org/Rebus unused, 631 files: **picked** (key RB; below the 702 that trial 61 called too large) |
| 8 | 8 | used; 9-87 as above; 88, 89 picked; 90 serilog/serilog-settings-configuration unused, 67 files: **picked** (key SSC) |

| Key | Repository | Commit | C# files | Setup |
| --- | --- | --- | --- | --- |
| NJ | ninject/Ninject | 01c86efb64f551e1f6a043ef2d5c03a2612918dd | 335 | Ninject.sln; library netstandard2.0;net462..net10.0 with StyleCop.Analyzers 1.1.118 + stylecop.json (header with decoration, newline at end omitted), TreatWarningsAsErrors; Nerdbank.GitVersioning; tests net6.0;net462 (net6.0 run: 748 pass, 1 fails untouched, 5 skipped) |
| RB | rebus-org/Rebus | 8600c812c9909c0a21906d8da734639b89478f30 | 631 | Rebus8.sln, no .editorconfig/props; Rebus netstandard2.0..net10.0, Rebus.Tests net10.0 NUnit |
| SSC | serilog/serilog-settings-configuration | df51082e11ceb7cc9fbc2bff15008b946ecdc616 | 67 | .editorconfig, Directory.Build.props (TreatWarningsAsErrors, Nullable, ImplicitUsings, docs), global.json 10.0.100; tests publish TestApp (its nuget.config clears the sources), 331 pass |

## Targeted (trial 61's fixes: init turns BRO1503 off for blank lines below type/namespace `{`; BRO1107/BRO1109 order)

No unused pool entry fits (the unused ones fail to build or are XAML/CSharpier). Scanned clones (`blankbrace.py`):
taglib-sharp (used) 104 with / 700 without, Embeddinator-4000 108/303 (needs mono), Cecil 551/463 (used, trial 61),
jbevain/mono.linq.expressions 65/15. No candidate had `(` alone on a line before an argument list in quantity (Xamarin.Auth:
needs workloads).

| Key | Repository | Commit | C# files | Why / setup |
| --- | --- | --- | --- | --- |
| MLE | jbevain/mono.linq.expressions | 7973f862ddfea552457c7b2bc457fb87c481a791 | 31 | Mono style like Cecil (blank line below most type/namespace `{`: init should turn BRO1503 off); netstandard2.0;net40 + netcoreapp2.1;net40 tests + a net40 dbg exe; Directory.Build.props is UTF-16 LE with BOM; net40 builds against `/usr/lib/mono/4.0-api` (linked to Microsoft.NETFramework.ReferenceAssemblies.net40 1.0.3: environment, not the repo); 75 tests on netcoreapp2.1 |
