# Trial 54: StackExchange/Dapper (targeted at trial 53's charset / final-newline notes)

main eb47546a, 157 .cs, root `.editorconfig` `[*.{cs,csx,vb,vbx}] charset = utf-8-bom`, `insert_final_newline = true`
(156 of 157 files with a BOM, 2 without a final newline: the minority case for both keys), `dotnet_sort_system_directives_first`.
TreatWarningsAsErrors in the root props, central package management with `GlobalPackageReference`s (ReferenceTrimmer,
Nerdbank.GitVersioning, SourceLink), frameworks net461/netstandard2.0/net8.0/net10.0 (libraries), net481/net8.0/net10.0
(tests), net472/net10.0 (benchmarks), net5.0 (Dapper.Rainbow), StrongName projects compiling the same files. No analyzers
of the style kind. Path A (init). StyleBro main dc107c4, 0.8.0-trial54.1; re-check on 0.8.0-trial54.2 (the fixes below).

Setup: `PackageVersion` in Directory.Packages.props, `PackageReference` in the root Directory.Build.props (tests/ and
benchmarks/ import it). Tests: `Dapper.Tests` net10.0, filter Sqlite|DuckDB (8 tests; the rest need SQL Server, MySQL,
Postgres, Firebird or Snowflake: of all 800, 66 pass without them, 704 fail on connections, 30 skipped).

## Results, first round (0.8.0-trial54.1, default engine)

| Step | Exit | Notes |
|---|---|---|
| base build | 0 | |
| base TWAE Debug/Release | 1 | 5 errors each, RT0000 (ReferenceTrimmer: "Enable /doc parameter", tests/ and benchmarks/ turn docs off) |
| base tests Debug/Release | 0 | 8/8 |
| `bro preview init` | 3 | Not verified (below) |
| init --write | 0 | **trial 53's notes fired**: "Charset: ... 1 of 156 C# files starts without a BOM: 'format' adds a BOM to it", "Final line breaks: ... 2 of 156 ... end without a line break"; the usings-order note (25 of 112) |
| `bro format` | 3 | 17 runs (7 frameworks, Release where `#if DEBUG` differs, shared-file groups); "Not verified": 3 projects didn't load |
| `bro check` | 3 | the same |
| `bro check --build` | 2 | Not clean: 8 findings in Debug and Release, all in Dapper.Tests.Performance's net472 code |
| TWAE Debug/Release | 1 | the same 5 RT0000 errors as the untouched code |
| tests | 0 | 8/8 |

The host engine (same build): preview, format, check 0; check --build 3 (Not verified: the builds fail as the untouched
code's do, RT0000); trees differ (the host also formatted the net472/net481 code).

## Findings

1. **format couldn't load a library without the run's framework when its packages have props and targets** (the soak's
   only real difference, a bug of the default engine). In the net472 run (Dapper.Tests.Performance), the net481 run
   (Dapper.Tests) and the net5.0 run (Dapper.Rainbow), the referenced Dapper (net461;netstandard2.0;net8.0;net10.0), and
   Dapper.ProviderTools/SqlBuilder/EntityFramework, failed: `The result "" of evaluating the value
   "$(ReferenceTrimmerTasksAssembly)" of the "AssemblyFile" attribute in element <UsingTask> is not valid`. The run's
   framework comes from the environment; `SelectFrameworkTargets` clears it right after the project file for projects that
   load as usual, but NuGet's `nuget.g.props` had already been imported with `TargetFramework=net472` (its ImportGroups
   are per framework: none matched), while `nuget.g.targets` saw the cleared framework and imported the outer build's
   `buildMultiTargeting/ReferenceTrimmer.targets`, whose task assembly the props define. The 3 projects that need Dapper
   were skipped ("references didn't load"), format said "Not verified" (honest) and `check --build` found 8 findings
   per configuration in the skipped net472 code (honest). The SourceLink message in the same log ("The URL of repository
   remote 'origin' is invalid": the trial clone's origin is a local path) is the trial setup's, not Dapper's.
   Fixed: `SelectFrameworkProps`, the same condition, imported through `CustomBeforeDirectoryBuildProps` (before
   Directory.Build.props and NuGet's props); a repository's own CustomBeforeDirectoryBuildProps wins
   (`Format_ALibraryWithoutTheRunsFramework_GetsItsPackagesPropsAndTargetsForTheSameBuild`: a props/targets pair in the
   library's folder; without the props the build fails as Dapper did).
2. **"benchmarks/Dapper.Tests.Performance/Linq2Sql/DataClasses.designer.cs is generated and checked in: rebuild and commit
   it."** The file is Visual Studio's LINQ to SQL designer output (next to DataClasses.dbml): no build writes it. Fixed:
   a `.designer.cs` next to a `.dbml`, `.xsd` (DataSet) or `.edmx` (EF6) is designer-written, like the `.resx`/`.settings`
   ones (`Format_ADesignersOutputNextToItsModel_IsntGeneratedByTheBuild`).
3. init's summary listed BRO1306 twice here too ("Turned 14 rules off"): Finbuckle's finding.

Not findings: the kept BRO1307 `s_converter` -> `Converter` ("the new name would mean something else"), the "Indentation
kept" note for SqlMapper.cs's `#if`-decided lines, the IDE0049 hold-back (SDK 10.0.1xx). `check --build` on the default
engine counted "107 errors the untouched code has too" in Debug, the host's run 5 (the same untouched commit; the first
round ran next to another chain's builds).
