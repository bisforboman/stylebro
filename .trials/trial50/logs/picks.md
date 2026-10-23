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

## Targeted (pool)

- **icsharpcode/SharpZipLib** (pool, trial 36): re-checks trial 49's ReSharper fix (`NamingRules.IsDisabledByReSharper`):
  file-wide `// ReSharper disable InconsistentNaming` in src/ICSharpCode.SharpZipLib/Core/ByteOrderUtils.cs and
  test/.../Core/ByteOrderUtilsTests.cs; no global.json. No pool entry has a global.json pinning SDK 8 or a known
  pre-existing CS8002 (probed: nodatime, castle Core, ClosedXML, shouldly, NJsonSchema, Havit.Blazor, WireMock.Net,
  SharpZipLib; WireMock.Net has 7 such files but 777 .cs files).

## Commits

| Repository | Branch | Commit |
|---|---|---|
| commandlineparser/commandline | master | 1e3607b97af6141743edb3c434c06d5b492f6fb3 |
| Cysharp/ObservableCollections | master | 8ed7ad87b35885b2540158d9188d710b1cf32763 |
| dahomey-technologies/Dahomey.Json | master | 43ddbf14d282eccb9802a040fc5d85e7a1422166 |
| icsharpcode/SharpZipLib | master | ff2d7c30bdb2474d507f001bc555405e9f02a0bb |
