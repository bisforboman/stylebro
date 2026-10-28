# Trial 59 picks (2026-10-28)

StyleBro main 1ab4981 (after #311). Variant: three random plus one targeted. Trial feed 0.8.0-trial59.1.

## Random (scripts/trials/random.json, 95 entries)

`rng = random.Random(59)`, `rng.randrange(95)` three times: **28, 10, 83**. Entries 0-75 are all in `used.tsv`.

| Draw | Entry | Result |
| --- | --- | --- |
| 28 | 28 jamesmh/coravel | used; 29-75 used; 76 SteveDunn/Vogen 702 .cs files (too large); 77 UglyToad/PdfPig 1,238 (too large); 78 xoofx/SharpYaml unused: **picked** (key SharpYaml) |
| 10 | 10 nissl-lab/npoi | used; 11-75 used; 76, 77 too large; 78 picked by draw 28; 79 adamralph/bullseye unused: **picked** (key Bullseye) |
| 83 | 83 danielpalme/ReportGenerator | unused: **picked** (key RG) |

| Key | Repository | Commit | C# files | Setup |
| --- | --- | --- | --- | --- |
| SharpYaml | xoofx/SharpYaml | 99fb455e15457eb04e65c3f65bd14f0291c4b5d8 | 245 | `src/SharpYaml.slnx`, net10.0/net8.0/netstandard2.0 library, in-solution source generator, `src/.editorconfig`, global.json in `src/` |
| Bullseye | adamralph/bullseye | 5fb351898d04b4c0f7c1fd542052c0b1c7c7b9c0 | 55 | `Bullseye.slnx`, TreatWarningsAsErrors, net8.0/net9.0/net10.0, PublicApiAnalyzers + VS Threading analyzers, Microsoft.Testing.Platform runner (global.json) |
| RG | danielpalme/ReportGenerator | eda6e6e9ac37c76d3d3dc1514fed9cfeba26bf86 | 276 | StyleCop.Analyzers 1.1.118 (migrate), `src/ReportGenerator.slnx`, net8.0/net9.0/net10.0 + a net47 console, MTP runner; global.json pins 10.0.403 (lowered) |

## Targeted (trial 58's fix: BRO1103/BRO1106 skip expression trees)

No unused pool entry fits (the six left fail to build or have no tests). Scanned (shallow clones: .cs files, files with
`Expression<Func`, `string.Empty` inside a lambda): LINQKit 66/21/0, nein-linq 95/15/0, DynamicExpresso 76/3/1,
Serialize.Linq 110/27/0, Gridify 132/18/2, Sieve 55/3/0, DelegateDecompiler 173/29/4, Remote.Linq 676/51/5 (too large),
EntityFrameworkCore.Projectables 157/9/2, Expressionify 38/2/1.

| Key | Repository | Commit | C# files | Why / setup |
| --- | --- | --- | --- | --- |
| DD | hazzik/DelegateDecompiler | bc42e9706ddc4ec19166e8512256a32047c635e9 | 173 | Decompiles IL into expression trees; its tests write `Expression<Func<Employee, bool>> expected = e => (e.FirstName ?? string.Empty)...` next to a compiled `Func<>` twin and compare. net40/net45/net8.0 tests, NUnit; EF Core tests (SQL Server) |

## Smoke

Not run: the session's auto-mode permission classifier refused to build and run the cloned repositories (twice), so the
trial is waiting for the owner.
