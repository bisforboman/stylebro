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
- `scripts/stylecop-survey`: generates `docs/stylecop-mapping.md` (inventory of StyleCop rules, repo surveys,
  SDK coverage check). Hand-written decisions in `decisions.psd1`, measured data in `data/`. Not part of the slnx.
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

## BRO11xx: readability (numbered in blocks like StyleCop: BRO11xx readability, BRO16xx documentation)

- **BRO1101** (SA1106) empty statements: `;` in blocks/switch sections and after a type's or namespace's `}`.
  Not reported: embedded (`while (x) ;`, left to the compiler's CS0642; a `{ }` fix would hide a likely bug),
  labeled (`end: ;`, removal can break the build), bodiless records. The fix is text-based and computed per line
  (`EmptyStatements.GetChanges`), so single fix and Fix All agree: whitespace-only lines go completely, otherwise
  the `;` takes the adjacent whitespace. Directives always have their own line, so no directive skip is needed.
- **BRO1102** (SA1133) combined attributes: `[A, B]` -> `[A]` + `[B]`, target repeated. Own line -> one line per
  attribute; shares a line with code -> `[A] [B]`. Like StyleCop, parameters/type parameters are not reported.
  Lists with comments between attributes are skipped (StyleCop's fix drops the comment).
- **BRO1103** (SA1131) constant on the left of a comparison: swap operands, flip `<`/`>`/`<=`/`>=`. "Constant" as in
  StyleCop (incl. `static readonly` fields). Only built-in operators and operators from the core library (same
  assembly as `object`) are swapped; a type's own `==` may not be symmetric -> not reported (deviation). Same-precedence
  left operand gets parentheses (`1 == 2 == b` -> `b == (1 == 2)`). Fix All is a bottom-up rewriter (nesting).
- **BRO1104** (SA1129) `new T()` for value types -> `default(T)`, or like StyleCop `CancellationToken.None`,
  `Guid.Empty`, `IntPtr/UIntPtr.Zero`, an enum's zero member; `default(T)` in parameter defaults (must be constant).
  Not reported: explicit parameterless struct ctor (like StyleCop), type parameters and `new S();` statements
  (deviations; StyleCop's fix for the statement doesn't compile). Needs the semantic model.
- For both, StyleCop and StyleBro were run on the same probe files: positions and fixed output are identical except
  the three documented deviations.
- Shared logic in `src/StyleBro.Analyzers/Readability/`, fixes in `src/StyleBro.CodeFixes/Readability/`.
- Tests use the generic `Verifier<TAnalyzer, TCodeFix>`; each skip condition was checked by disabling it and
  confirming a test fails.

## BRO15xx: layout

- **BRO1501** (SA1509) blank line before `{` and **BRO1502** (SA1510) blank line before `else`/`catch`/`finally`.
  Same results as StyleCop, verified by running both on the same probe files (14 identical positions).
- "Blank lines directly above": only whole whitespace lines inside the token's leading trivia count, so string
  contents are never touched; the scan stops at a comment or directive line. The fix deletes those lines.
- StyleCop's exception, matched on purpose (user's decision): a `{` whose previous token is `}` is not reported
  (block after block, even with a comment in between). A standalone block after `;` IS reported, like SA1509.
  `do ... while`'s `while` is not a BRO1502 keyword.
- One analyzer (`Layout/BlankLineBeforeAnalyzer`, a single token pass) and one fix for both IDs.

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
- **BRO1101/BRO1102** (2026-09-30): 0 findings in OpenTelemetry, Polly and the private app, which all keep SA1106
  and SA1133 on (so StyleBro is not stricter than StyleCop there; BRO1001 firing 674x in Polly confirmed the hook
  loaded). FFMpegCore and Serilog: 0 findings. Newtonsoft.Json: 66 findings (3 BRO1101, 63 BRO1102) in 4 files, all
  fixed in one pass, no new compile errors, second run clean, diffs reviewed by hand.
- **BRO1501/BRO1502** (2026-09-30): 0 in the three StyleCop repos. First version flagged block-after-block too
  (22 of 23 real findings); StyleCop doesn't, so that was fixed. Now: FFMpegCore 2, Serilog 1, Newtonsoft.Json 0,
  all standalone blocks after a statement; fixed, builds, second run clean.
- **BRO1103/BRO1104** (2026-09-30): 0 in Polly and OpenTelemetry. Private app: 1 BRO1104 on a target-typed
  `new()` (`DateTime X { get; set; } = new();`), which StyleCop 1.1.118 predates and misses; the 1.2 beta reports it.
  FFMpegCore 1, Serilog 6, Newtonsoft.Json 30. All fixed in one pass, builds, second run clean. Hooking analyzers into a repo
  without editing it: set the env var `CustomAfterMicrosoftCommonTargets` to a targets file with `<Analyzer>` items.

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
4. Scope = migration parity, not "port every StyleCop rule". `docs/stylecop-mapping.md` (draft, 2026-09-30) maps
   all 197 StyleCop diagnostics to SDK / StyleBro / drop, from: the rule inventory read from the StyleCop DLLs
   (1.1.118 + 1.2.0-beta.556), config surveys and all-rules-on counts in OpenTelemetry, Polly and the private app,
   and an SDK check (75 rules: 49 fixed by `dotnet format` with SDK settings alone). Key findings:
   - `dotnet format` cannot fix naming (IDE1006: "doesn't support Fix All"), so a StyleBro rename fix is a real gap.
   - Top StyleBro candidates: ~~SA1133~~ (BRO1102), ~~SA1106~~ (BRO1101), ~~SA1509/SA1510~~ (BRO1501/BRO1502), ~~SA1131~~ (BRO1103), ~~SA1129~~ (BRO1104) (kept by
     all 3 teams), SA1128, SA1005, SA1413, SA1122, SA1116/SA1117, blank-line rules (SA1516, SA1505, SA1515,
     SA1512, SA1518).
   - 107 rules untested yet, mostly documentation (SA16xx).
   - The preset only claims IDE0011 + IDE0055 today; the SDK settings verified in the check should go into it.
     Some are opinionated (SA1101 `this.` is off in 2 of 3 repos), so decide per setting.
5. Documentation rules (BRO16xx): XML doc stubs, `<inheritdoc/>` on overrides/interface implementations,
   `<param>` kept in sync with the parameters. These need the semantic model.
6. StyleCop migration tool: `stylecop.json` + rulesets -> equivalent `.editorconfig` (the mapping is its spec).
7. Later: blank-line layout rules (the SDK's IDE2000 series is only experimental), baseline support
   (fail only on new violations).
