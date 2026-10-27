# Trial 57 picks (2026-10-27)

Variant: three random plus one targeted ("Mostly random", owner's choice 2026-10-23).

## Random (scripts/trials/random.json, 79 entries, rng = random.Random(57), randrange(79) three times)

Draws: 5, 47, 75. Used = in scripts/trials/used.tsv as of trial 56's PR (#308) or picked earlier in this trial.

- Draw 5 (RicoSuter/NSwag): used; 6-64 all used; 65 dotnet/dotNext: 1160 C# files (too large: "tens to a few hundred");
  66 fluentmigrator/fluentmigrator: 1248 C# files, net48 in most projects, database tests (too large); 67 Fody/PropertyChanged:
  net48;net10.0 in every project (a host repository); **68 JamesNK/Newtonsoft.Json.Bson** (51 C# files) at 083ffaaf5e5bf69c3bdcbfeb0cfd986aea4dd119.
- Draw 47 (sebastienros/esprima-dotnet): used; 48-64 used; 65-67 skipped above; 68 picked by draw 5; **69 microsoft/OpenAPI.NET**
  (569 C# files) at b0a6859be48927746726e967f578f370cd756f35.
- Draw 75: **sergeyteplyakov/ErrorProne.NET** (138 C# files, analyzers + code fixes, NUnit) at b57da22a2751e091822637ad97ecbbdbe2900aca.

## Targeted at trial 56's fixes

Lenses: (a) item functions / Regex.Replace(%(...)) in MSBuild files that fail once the repository has been built (#308's MSB4186
retry); (b) many split argument lists with commas starting lines (#308's BRO1117 fix). No pool entry fits either (scanned).
Scanned shallow clones (`scan.py`: lines starting with a comma in .cs, `%(` inside `$([...]::...)` in MSBuild files):
dotNext, fluentmigrator, PropertyChanged, Json.Bson, OpenAPI.NET, ErrorProne.NET, DiscordChatExporter, refit, splat, Akavache,
Fusillade, punchclock, Sextant, ReactiveUI, ReactiveUI.Validation, reactivemarbles/{PropertyChanged,ObservableEvents,extensions,
Mvvm,CacheDatabase,Locator}, NPoco, LightInject, CommandLineUtils, Vogen, PdfPig, SharpYaml, Humanizer, GuardClauses, linq2db,
TUnit, EFCore.BulkExtensions, LINQKit, Mapster, Dapper.Contrib, MySqlConnector, Respawn, EntityFramework.Exceptions,
EntityFrameworkCore.Projectables, echo-process, LiteDB, FreeSql, EventFlow, EventSourcing.NetCore, DuckDB.NET, DapperAOT,
fast-member, efcore.pg, MiniProfiler, nhibernate-core, CleanArchitecture (and 13 used ones skipped by used.tsv).
Leading commas in code are rare: Mapster has 30 (ExpressionDebugger.Tests) but was used in trial 11; linq2db (5253 files),
FreeSql (1210) and nhibernate (6757) are too large; the rest are SQL inside strings.

- **DapperLib/DapperAOT** at 93d99d36dd0142a07bf1b00567351f89b8dddb72 (lens a): test/Dapper.AOT.Test/Dapper.AOT.Test.csproj
  sets `DependentUpon` with `$([System.String]::Copy(%(Filename)).Replace(...))` (unquoted metadata in a property function, the
  same shape as DynamicData's `Regex.Replace(%(Filename), ...)`); slnx, CPM, Nerdbank.GitVersioning, global.json 10.0.203,
  nuget.config with `<clear/>` and packageSourceMapping `*` (the trial feed needs a `StyleBro.*` mapping), net48/net6.0/net8.0/net10.0
  tests, a Roslyn generator/analyzer in the solution. Run bro before and after a build.

## Smoke builds (Debug, SDK 10.0.113 + 8.0.132, DOTNET_ROLL_FORWARD=Major)

- Json.Bson: Src/NuGet.Config's myget feed is blocked by the proxy (removed locally); builds; 171 tests x2 (net6.0, net8.0) pass, net46 doesn't run on Linux.
- OpenAPI.NET: builds; MTP (`dotnet test --solution`, from the root for global.json), 1945 tests pass.
- ErrorProne.NET: src/NuGet.config's dnceng dotnet-tools feed is blocked (removed locally); builds; 418 tests pass (net9.0).
- DapperAOT: builds; `dotnet test test/Dapper.AOT.Test -f net10.0 --filter FullyQualifiedName!~Integration` (the repo's CI step;
  Dapper.AOT.Test.Integration needs Docker/Testcontainers, left out): 372 pass.
