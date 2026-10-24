# Trial 54 picks (2026-10-24)

StyleBro main dc107c4 (after #301), feed 0.8.0-trial54.1.

## Random (seed 54)

- Rule: `.trials/README.md`'s "three random plus one targeted" variant, ONE generator: `rng = random.Random(54)`,
  `rng.randrange(60)` three times (60 entries after trial 53's appends): **58, 8, 28** (0-based).
- Unused: indices 57-59 (Sleet, Finbuckle.MultiTenant, Flurl, appended and smoke-built by trial 53); 0-56 are in `used.tsv`.
- Walks:
  - Draw 58: **58 Finbuckle/Finbuckle.MultiTenant** (unused).
  - Draw 8: 8-56 used, **57 emgarten/Sleet**.
  - Draw 28: 28-56 used, 57-58 picked, **59 tmenier/Flurl**.
- After this trial every entry is used: trial 55 needs new entries appended first (see the end of this file).

## Targeted

Lenses (trial 53's fixes): a `.nupkg` committed next to C# code (`IsExtractedPackage` now needs the folder's name to match
the package), and an `.editorconfig` `charset` / `insert_final_newline` that a minority of committed files don't follow
(`Conventions.ConvertedMinorityFileNote`). No unused pool entry has either (the unused ones all fail to build).

Scanned (`scan.py`, shallow clones, unused only): Fody/Fody, Fody/PropertyChanged, NuGet/NuGetGallery (5 test .nupkg files
next to C# code, but 4,199 .cs, too large, and .NET Framework), Particular/NServiceBus.Persistence.Sql, dotnet/docfx (831 .cs),
dotnet/sourcelink (charset utf-8-bom: 128 of 132 with a BOM, 1 without a final newline; Arcade SDK from Azure DevOps feeds,
blocked by the proxy), fluentmigrator (1,248 .cs), microsoft/MSBuildLocator (15 .cs), nuke-build/nuke (646 .cs, 347 BOMs:
not a minority), oleg-shilo/cs-script. Of the random picks, Sleet has `charset = utf-8` with no BOMs (follows it), Finbuckle
and Flurl set neither.

- **StackExchange/Dapper** (main eb47546a, 157 .cs, not in the pool, added): root `.editorconfig` `[*.{cs,csx,vb,vbx}]
  insert_final_newline = true`, `charset = utf-8-bom`; 156 of 157 C# files have a BOM (`tests/Dapper.Tests/PreferTypeHandlersForEnumsTests.cs`
  doesn't), 2 lack a final newline (`benchmarks/Dapper.Tests.Performance/EntityFrameworkCore/EFCoreContext.cs`,
  `.../Linq2DB/ConnectionStringSettings.cs`): the minority case for both keys. TreatWarningsAsErrors, central package
  management, net461/netstandard2.0/net8.0/net10.0, PublicAPI analyzers, Nerdbank.GitVersioning (full clone). Tests: most
  need SQL Server/MySQL/Postgres/Firebird/Snowflake; the offline ones are the Sqlite and DuckDB providers (net10.0).

## Commits

| Repository | Branch | Commit |
|---|---|---|
| Finbuckle/Finbuckle.MultiTenant | main | 3765bade0735b09fb0399802d6d592d86335441e |
| emgarten/Sleet | main | 833134d176a6781b1fa3d5f902eee49720184ccf |
| tmenier/Flurl | dev | 36a3fa2b7e29ba1e44a39edfd0d4ae36284a6c75 |
| StackExchange/Dapper | main | eb47546a408bbf6c178bda4f04dc205bc51ffbfe |
