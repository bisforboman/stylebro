# Trial 65 picks (2026-11-02)

Sonnet experiment. Variant: three random plus one targeted. SDK 10.0.114 + 8.0.133. Tested: main 3ababb6 (after #322).

## Random (scripts/trials/random.json, 104 entries when drawn)

`rng = random.Random(65)`, `rng.randrange(104)` three times: **53, 36, 36**. Entries 53 (EasyNetQ, trial 52) and 36 (Pidgin,
trial 48) are used. Walking down: 54-91 used; 92 PolySharp needs SDK 11 (fails); 93 Statiq.Framework too large (857 files);
94-100 used; 101 serilog-sinks-console: **picked** (draw 1, key SC). Draw 2 (36): 92, 93 fail, 101 picked, 102
stefanprodan/AspNetCoreRateLimit: **picked** (key AR). Draw 3 (36): 103 thomaslevesque/WeakEvent has 11 .cs files (below
"tens", the smallest accepted so far is 19): skipped; the list ran out, so entries 104-105 were appended
(dynamicexpresso/DynamicExpresso, jwt-dotnet/jwt, from a GitHub search of C# repositories with 400-3000 stars, not in
used.tsv) and 104 DynamicExpresso (76 files) is **picked** (key DE).

## Targeted (trial 64's fix: BRO1519 skips the gap above a checked/unchecked statement when CSharpier lays the code out)

Searched GitHub code for `filename:dotnet-tools.json csharpier` (484 unused repositories), cloned the plausible small ones
and grepped for `checked {`/`unchecked {`. Without CSharpier: Geo (9), Imposter (143). With CSharpier: FlameCsv (413 files,
`.csharpierrc.yaml`, CSharpier 1.1.1 as a local tool; `checked { }` statements in test/Lib/BoundedMemory.Creation.cs right
after a declaration, plus unchecked in the tokenizers), Gulliver (1), SshAgentLib (1). dunet and the dailydevops
repositories (in the pool) are used. **ovska/FlameCsv picked** (key FC). Pool entries for the picks are added in the PR.
