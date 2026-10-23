# Trial 50 picks (2026-10-23)

## Random (seed 50)

- `scripts/trials/random.json` had 40 entries, all in `used.tsv`. Appended 10 (entries 41-50, alphabetically, all checked
  with `git ls-remote`): commandlineparser/commandline, Cysharp/ObservableCollections, dahomey-technologies/Dahomey.Json,
  DotNetAnalyzers/IDisposableAnalyzers, JasperFx/oakton, mgravell/Pipelines.Sockets.Unofficial, ncalc/ncalc,
  sebastienros/esprima-dotnet, Tyrrrz/Onova, zzzprojects/System.Linq.Dynamic.Core. List length now 50.
- `rng = random.Random(50)`; `rng.randrange(50)` three times: **31, 17, 23** (0-based indices).
  - Draw 31: indices 31-39 used; walks to index 40 (entry 41) **commandlineparser/commandline**.
  - Draw 17: indices 17-39 used, 40 already picked; walks to index 41 (entry 42) **Cysharp/ObservableCollections**.
  - Draw 23: indices 23-39 used, 40-41 picked; walks to index 42 (entry 43) **dahomey-technologies/Dahomey.Json**.
- Smoke check (fixer, before the picks were fixed; SDK 10.0.113, `DOTNET_ROLL_FORWARD=Major`, `NuGetAudit=false`,
  `EnableWindowsTargeting=true`): `dotnet test` on each solution passes untouched: commandline 684 (netcoreapp3.1),
  ObservableCollections 24 (net6.0) + 10 (R3, net8.0), Dahomey.Json 360/372/374 (netcoreapp3.1/net5.0/net6.0).

## Targeted

- First choice icsharpcode/SharpZipLib (pool) was withdrawn: it is in `used.tsv` (trial 36), and trials use only
  repositories never used before (parent's note).
- Every usable pool entry is used; no pool entry has a global.json pinning SDK 8 or a known pre-existing CS8002. New
  candidates probed for `// ReSharper disable ... InconsistentNaming` (shallow clones, grep): DotNetAnalyzers/IDisposableAnalyzers
  (15 files) rejected: its tests are net6.0-windows with WPF (NETSDK1100 off Windows); ststeiger/PdfSharpCore (36 files,
  674 .cs) rejected: only 32 tests, 5 fail untouched (no ghostscript); NetTopologySuite, PdfPig, NAudio, WireMock.Net
  have suppressions but 780-1,400 .cs files.
- **ncalc/ncalc** (random.json entry 47, taken as the targeted pick, so later draws walk past it): re-checks trial 49's
  ReSharper fix (`NamingRules.IsDisabledByReSharper`): `// ReSharper disable once InconsistentNaming` above the public
  `MathHelper.IEEERemainder` (src/NCalc.Core/Helpers/MathHelper.cs:265). 150 .cs, netstandard2.0;net8.0;net10.0, TUnit
  (MTP) on net10.0. Smoke check: needs a full clone (Nerdbank.GitVersioning); global.json 10.0.101 lowered to 10.0.100;
  `dotnet test --solution NCalc.slnx`: 901 passed.

## Commits

| Repository | Branch | Commit |
|---|---|---|
| commandlineparser/commandline | master | 1e3607b97af6141743edb3c434c06d5b492f6fb3 |
| Cysharp/ObservableCollections | master | 8ed7ad87b35885b2540158d9188d710b1cf32763 |
| dahomey-technologies/Dahomey.Json | master | 43ddbf14d282eccb9802a040fc5d85e7a1422166 |
| ncalc/ncalc | master | 9e875d92aaf7110d66374a98a628662698d55908 |
