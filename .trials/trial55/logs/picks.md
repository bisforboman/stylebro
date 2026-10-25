# Trial 55 picks (2026-10-25)

StyleBro main 09d42be (after #303: the format host is the default engine), feed 0.8.0-trial55.1.

## Random (seed 55)

- Rule: `.trials/README.md`'s "three random plus one targeted" variant, ONE generator: `rng = random.Random(55)`,
  `rng.randrange(64)` three times (64 entries after trial 54's appends): **11, 25, 19** (0-based).
- Unused: indices 60-63 (ZLinq, Newtonsoft.Json.Schema, FeatureManagement-Dotnet, FusionCache, appended and smoke-built by
  trial 54); 0-59 are in `used.tsv`.
- Walks:
  - Draw 11: 11-59 used, **60 Cysharp/ZLinq**.
  - Draw 25: 25-59 used, 60 picked, **61 JamesNK/Newtonsoft.Json.Schema**.
  - Draw 19: 19-59 used, 60-61 picked, **62 microsoft/FeatureManagement-Dotnet**.
- After this trial only entry 63 (FusionCache) is unused.

## Targeted

Lens (trial 54's fixes): a multi-targeted repository where a project targets a framework one of its project references
lacks, with a package that brings props and targets (ReferenceTrimmer in Dapper; here Nerdbank.GitVersioning), or LINQ to
SQL / DataSet / EF6 designer files. No unused pool entry has it (FastEndpoints, Verify, bUnit, Nerdbank.GitVersioning,
vs-mef fail to build; DiscordChatExporter is a CSharpier/Avalonia app).

Scanned (`scanmt.py`, shallow clones; used ones skipped: BitFaster.Caching, RecyclableMemoryStream, sourcelink, Tomlyn,
ConsoleAppFramework, Ulid, ZString, ObservableCollections, vs-validation, vs-threading, Nerdbank.MessagePack, slngen):
MiniProfiler (181 .cs, no such package), StackExchange.Exceptional, StackExchange.Utils, ReferenceTrimmer (its own repo:
single-targeted), Library.Template (4 .cs), MasterMemory, NativeMemoryArray, R3, SimdLinq, UnitGenerator (no package),
StackExchange.Metrics (84 .cs, NB.GV + SourceLink, last commit 2022, global.json 6.0.101: kept as the fallback),
NetEscapades.EnumGenerators, Ben.Demystifier (36 .cs), SponsorLink, DotNext (1,139 .cs), ThisAssembly (14 .cs), OpenCQRS,
fast-member, Utf8Json, sqids-dotnet (14 .cs). No designer files next to .dbml/.xsd/.edmx in any of them.

- **protobuf-net/protobuf-net.Grpc** (main 3471179c, 164 .cs, not in the pool, added): test projects target
  net472;net8.0;net10.0 and reference protobuf-net.Grpc.AspNetCore (net8.0;net10.0) and the test proxy (net8.0;net10.0),
  the ClientFactory library (netstandard2.0;netstandard2.1;net8.0) lacks net10.0 and net472; Nerdbank.GitVersioning (full
  clone) and PublicApiAnalyzers in every project; a `VS` solution configuration; F# and VB examples in the solution;
  nuget.config `<clear />` (with a `<clear/>` in a comment above it).

## Commits

| Repository | Branch | Commit |
|---|---|---|
| Cysharp/ZLinq | main | f7591df24c47b7c4a262c1e78241c594fbeb7286 |
| JamesNK/Newtonsoft.Json.Schema | master | 5456c4bffe9a0b18ecd70ee056fc43f8a19ebf7a |
| microsoft/FeatureManagement-Dotnet | main | 787f819938ce6893452c04ff3088b11f54943a61 |
| protobuf-net/protobuf-net.Grpc | main | 3471179ce47470ed8858c9f5cb69314c5e00dbcc |
