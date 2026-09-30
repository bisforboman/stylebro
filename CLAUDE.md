# StyleBro

Roslyn analyzers + code fixes for C#: a modern replacement for StyleCop.Analyzers
(last stable release 1.1.118, stuck in 1.2.0-beta for years). Name: **StyleBro** (the friendly successor to StyleCop: it fixes things for you instead of writing tickets). Rule prefix `BRO`, config keys `stylebro_*`.
The project was renamed from "Kempt" (pure text/file-name replacement). Repo: `github.com/bisforboman/stylebro`.

## Core idea

Only analyzers **with code fixes that `dotnet format` can apply**. The goal is that running
`dotnet format` fixes everything StyleBro reports. Formatting-only rules are out of scope where the SDK
already covers them (IDE rules, `.editorconfig`); the preset enables those built-in rules instead.

## Status

Re-verified after the rename to StyleBro (clean tree, SDK 10.0.401, 2026-09-29): `dotnet build StyleBro.slnx`
(0 warnings), `dotnet test StyleBro.slnx` (11/11 passed), `scripts/verify-format.ps1` (both passes OK, output
matches `Expected/`) and `dotnet pack src/StyleBro.Package` (`StyleBro.Analyzers.0.1.0-alpha.1.nupkg` with both
DLLs, targets and globalconfig) are all green. The rename needed no fixes. Since then: real-world testing
(see the log below), 19 unit tests, all green.

## Layout

- `src/StyleBro.Analyzers`: netstandard2.0, references Microsoft.CodeAnalysis.CSharp only.
  Must never reference Workspaces (RS1038).
- `src/StyleBro.CodeFixes`: netstandard2.0, Workspaces, code fixes + Fix All providers. References Analyzers.
- `src/StyleBro.Package`: packs both DLLs into `analyzers/dotnet/cs`, plus `build/StyleBro.Analyzers.targets`
  that adds `stylebro.recommended.globalconfig` (global_level -1; opt out with `<StyleBroPreset>none</StyleBroPreset>`).
- `tests/StyleBro.Tests`: xunit + Microsoft.CodeAnalysis.CSharp.CodeFix.Testing 1.1.2 (`DefaultVerifier`).
- `samples/Messy`: dotnet format integration sample. `Input/*.cs` is copied to `Generated/` (gitignored),
  formatted, compared with `Expected/`, then a second run with `--verify-no-changes` must pass.
- Central package management in `Directory.Packages.props`. Roslyn pinned to **4.8.0** for broad SDK/VS
  compatibility; don't raise it without a reason.
- `.github/workflows/ci.yml`: ubuntu-latest, .NET 10; runs test, verify-format, pack, uploads the nupkg.
- `.github/workflows/release.yml`: on a `v*` tag, runs the same checks, packs with the version from the tag
  (`v0.1.0-alpha.1` -> `0.1.0-alpha.1`; overrides `<Version>` in the csproj), pushes to nuget.org via Trusted
  Publishing (`NuGet/login@v1`, no stored API key) and creates a GitHub release (prerelease if the version has a `-`).
  Set up: nuget.org Trusted Publishing policy (repo owner bisforboman, repo stylebro, workflow `release.yml`,
  environment `release`, packages `StyleBro.*`, new packages allowed), repo *variable* `NUGET_USER` = `bisforboman`
  (nuget.org profile name; a variable, not a secret, so logs aren't masked), and the `release` environment
  requires the owner's approval. The first failure (HTTP 401
  "No matching trust policy") was simply a missing policy.

## Design rules for every rule

1. Every diagnostic has a code fix, and **Fix All works**. `dotnet format` applies fixes through Fix All.
   Prefer a custom `FixAllProvider.Create(...)` (document-based rewrite) over `WellKnownFixAllProviders.BatchFixer`
   when edits can overlap.
2. Fixes are **deterministic and idempotent**: after a fix, the analyzer reports nothing, and a second
   `dotnet format` run changes nothing. Analyzer and fix share one piece of logic so they can't disagree.
3. When a fix could be unsafe (e.g. preprocessor directives between members), skip the case: no diagnostic
   rather than a wrong fix.
4. Configuration goes through `.editorconfig` keys prefixed `stylebro_`, read via `AnalyzerConfigOptionsProvider`
   in both the analyzer and the fix.
5. Every rule gets a `docs/rules/BROxxxx.md`, a row in `AnalyzerReleases.Unshipped.md`, unit tests covering
   the single fix + Fix All, and a case in `samples/Messy`.

## BRO1001: member ordering (implemented, unit + dotnet format tests pass)

- Replaces SA1201-SA1204 and SA1214. Sort key: kind -> accessibility -> const -> static -> readonly.
  Defaults match StyleCop so migration is painless (kind order verified pairwise against StyleCop 1.1.118).
- One diagnostic per type, on the first out-of-place member. The fix sorts the whole type.
- Trivia: blank-line layout stays with the position; comments, docs and attributes move with the member.
- The Fix All rewriter sorts nested types before their parents in one pass.
- Types with directives between members (`#region`, `#if`, `#pragma`) are skipped.
- Types where sorting would swap dependent field/auto-property initializers are skipped (`InitializerOrder.cs`).
- Code: `src/StyleBro.Analyzers/Ordering/MemberOrdering.cs` (shared logic), `MemberOrderOptions.cs`,
  `MemberOrderingAnalyzer.cs`, `src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs`.

## Real-world testing log

- **FFMpegCore** (open source, 6 projects, 180 files, no StyleCop; 2026-09-29):
  27 diagnostics in 26 files. One `dotnet format` run fixed all 26 files, each diff a pure line permutation;
  the solution built with TreatWarningsAsErrors, and `--verify-no-changes` passed. Diffs reviewed by hand: docs,
  attributes and the blank-line layout are handled correctly.
- **Bug (fixed): initializer order.** Sorting changed the order initializers run in:
  `static int A = 5; static readonly int B = A + 1;` was reordered so B became 1 instead of 6, with no warning.
  Now `Ordering/InitializerOrder.cs` (called from `MemberOrdering.GetKeys`, so analyzer and fix agree) skips a type
  when the sort would swap two static or two instance initializers and either one mentions a member of the type or
  creates an instance of it (syntax only, conservative; ignores `typeof`, `nameof`, `other.Name`). Covered by
  6 unit tests and `samples/Messy/Input/Initializers.cs`. FFMpegCore results unchanged (no new skips).
- **Gotcha:** `<WarningsNotAsErrors>BRO1001</WarningsNotAsErrors>` makes `dotnet format` skip the rule entirely
  (0 files formatted), while the build still reports it. `TreatWarningsAsErrors` alone is fine. Documented in BRO1001.md.
- **StyleCop comparison on a private app** (30 projects, ~6,500 tests, StyleCop.Analyzers 1.1.118 with defaults,
  no stylecop.json; 2026-09-29). The ordering rules SA1201-SA1204 and SA1214 were all enabled as warnings for the test.
  - Before: 1076 StyleCop ordering diagnostics (SA1201 839, SA1202 185, SA1204 50, SA1214 2), 877 BRO1001.
  - One `dotnet format` pass: 868 files, all line permutations, second run no changes, 27 s. Build 0 errors,
    all of the app's tests pass.
  - After: StyleCop quiet everywhere except two large files that use `#region`, which StyleBro skips on purpose.
    StyleBro flagged no file StyleCop was happy with.
  - Found and fixed: (1) default order had operators after methods; StyleCop wants conversion operators, then
    operators, then methods. Added the `conversion` kind. The full 14-kind order was checked against StyleCop
    with all 182 pairs (both orders), 0 differences. (2) The initializer guard treated constants as state and
    skipped a type for `Prop { get; set; } = SomeConst`; constants are now ignored.
  - Not covered by StyleBro (by design): namespace-level ordering of types (part of SA1201).

## Known open questions

- Answered: `dotnet format` does pick up code fixes from analyzers referenced as
  `ProjectReference OutputItemType="Analyzer"` (as in `samples/Messy`).
- Answered: help links and `PackageProjectUrl` point to `github.com/bisforboman/stylebro`.
- Name checked 2026-09-29: NuGet IDs `StyleBro` and `StyleBro.Analyzers` are free (0 search hits);
  a `stylebro` GitHub user/org is free; no similar C# projects on GitHub. `StyleBro.Analyzers` is now taken by us.

## Next steps (in order)

1. ~~Make everything build and pass (tests + verify-format).~~ Done, re-verified after the rename.
2. Real-world testing: run `dotnet format analyzers --diagnostics BRO1001` on some real repos and check they
   still build and the second run changes nothing. Compare with StyleCop's SA1201-SA1204 on a repo that uses
   StyleCop; differences are bugs or deliberate, documented choices.
3. ~~Check the name, create the repo, publish.~~ Done: public at `github.com/bisforboman/stylebro` (MIT);
   `StyleBro.Analyzers 0.1.0-alpha.1` published to nuget.org on 2026-09-30 by release.yml (tag at `bc053c2`).
   To release: bump nothing in the csproj, just push a tag `vX.Y.Z[-suffix]` and approve the `release` environment.
4. Documentation rules (BRO16xx): XML doc stubs, `<inheritdoc/>` on overrides/interface implementations,
   `<param>` kept in sync with the parameters. These need the semantic model.
5. StyleCop migration tool: `stylecop.json` + rulesets -> equivalent `.editorconfig`.
6. Later: blank-line layout rules (the SDK's IDE2000 series is only experimental), baseline support
   (fail only on new violations).
