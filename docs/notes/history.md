# Project history

The old Status section's release history and the dated "Added ..." sections (rules, guards, performance rounds, CI changes and lessons, 2026-10-04 to 2026-10-10). Moved from CLAUDE.md on 2026-10-23.

## Release history (the old Status section)

**Stable 1.0 (owner's decisions, 2026-10-11, docs/decisions/):** 1.0 ships after two clean trials in a row (backlog
item "Two clean trials before 1.0" keeps the tally); after 1.0, new rules stay off until a preset version opts in
(planned, backlog "Preset versions"). What patch/minor/major may change: `docs/versioning.md`; releases themselves still
follow the release policy below (the owner says when). CHANGELOG.md is the full release history (each version's
section is its GitHub release notes), SECURITY.md points to private vulnerability reporting.

Re-verified after the rename to StyleBro (clean tree, SDK 10.0.401, 2026-09-29): `dotnet build StyleBro.slnx`
(0 warnings), `dotnet test StyleBro.slnx` (11/11 passed), `scripts/verify-format.ps1` (both passes OK, output
matches `Expected/`) and `dotnet pack src/StyleBro.Package` (`StyleBro.Analyzers.0.1.0-alpha.1.nupkg` with both
DLLs, targets and globalconfig) are all green. The rename needed no fixes. Since then: real-world testing
(see the log below) and 71 rules; 397 unit tests (incl. every doc example), all green (2026-10-02). Since
2026-10-05: 118 rules, 842 tests. Migration tool
`bro` added (2026-10-01, see below).
0.7.0-alpha.1 (2026-10-13, 129 rules: preset versions (`StyleBroPreset` X.Y/latest/none), StyleBro + CSharpier
settle (`stylebro_layout_by`, `stylebro_layout_print_width`), naming rules only when enforced, verify/preview with
in-solution generators, MSBuild-evaluated StyleCop conditions, leftover StyleCop config, trial 13-14 fixes; owner's call) is the latest on
nuget.org (released 2026-10-13; `StyleBroSelfVersion` is 0.7.0-alpha.1). Before it:
0.6.0-alpha.1 (2026-10-12, 129 rules: `--json`/`format --files`/`--agents-md` for agents, Sonar-matching settings,
init follows many more conventions, the preset no longer sorts usings, trials 6-12 fixes; owner's call "Release the
alpha"). Before it:
0.5.0-alpha.1 (2026-10-10: kept findings with reasons and a clean check, init explains its adoptions and needs
3 places, the trial-4 fixes; owner's call). Before it: 0.4.0-alpha.1 (129 rules: BRO1008 replaces IDE0065, BRO1149-BRO1151 mirror Sonar; Sonar setups in migrate,
init adapts to the repo's conventions, public API not renamed by default; owner's call, 2026-10-10) is the latest on
nuget.org once its tag runs; bump `StyleBroSelfVersion` after it's live. Before it: 0.3.0-alpha.1 (125 rules incl. BRO1410, 11 options, `--diff` preview, docs site help links, both first-run trials'
fixes; owner's call, 2026-10-09) is the latest on nuget.org once its tag runs; bump `StyleBroSelfVersion` after it's
live. Before it: 0.2.0-alpha.1 (124 rules, `init`/`format`/`init --modernize`, the preset finally reaching the compiler, the
first-run fixes; owner's call "a new minor version", 2026-10-06) is the latest on nuget.org with StyleBro.Migrate (now StyleBro.Tool);
after it's live, bump `StyleBroSelfVersion` in its own PR (the preset then applies to StyleBro's own code: run
`dotnet format` and commit that). Earlier: alpha.8 (71 rules + baseline support + the BRO1117 crash fix, 2026-10-02), alpha.7 (71 rules), alpha.6 (54 rules,
the first prerelease published without an approval step), alpha.5 (43 rules + the tool), alpha.4 (44 rules), alpha.3 (18 rules). From alpha.5 on, release.yml also packs `bro` (package StyleBro.Tool, a .NET
tool) with the same version; CI packs both too.

**Release policy (user's decision, 2026-09-30):** don't publish a version for every batch of rules while the project
is starting out. Keep adding rules on `main` and release again once there's a critical mass of new rules. Don't
suggest or push a release tag after each batch. Since 2026-10-03 (user: "don't focus on releases yet, I'll let you know
when we can do that"): don't suggest releases at all; the user says when.

## Added 2026-10-04/05 (performance, superseding StyleCop, beyond StyleCop)

Decisions for all of this are in `docs/decisions.md`; rule details on the rule pages.

**Performance** (owner: "performance is an important metric for analyzers"):
- `TreeWalk` (src/StyleBro.Analyzers/TreeWalk.cs): one walk per tree, tokens and nodes cached as arrays in a
  ConditionalWeakTable (trivia from the tokens, in document order). A bare walk of Newtonsoft.Json costs ~35-40 ms,
  reading the array ~2 ms; ~16 analyzers used to walk every file themselves. Use `TreeWalk.Tokens/Trivia/Nodes(root)`
  for every whole-tree walk. 595 -> 386 ms total, same diagnostics.
- **Syntax-only analyzers use `context.RegisterNodeAction`, not `RegisterSyntaxNodeAction` (2026-10-11, NodeActions.cs):
  same action and nodes, run from a tree action. Node and symbol actions make the driver compile every method body
  first, and `dotnet format` runs the analyzers once per fixed id on every project: that binding was most of a
  real-world run (Newtonsoft.Json ~4 s per compilation and id against <1 s). Analyzers that query the semantic model
  keep `RegisterSyntaxNodeAction`/symbol actions: from a tree action their queries re-bound in builds (2-4x slower).
  Fix All providers compute documents/renames in parallel (`LinkedFileFixAllProvider.ForEachAsync`, results used in
  order): fixes must only read the solution and keep shared caches thread-safe.
- Cheap syntax checks before semantic work everywhere (CamelCaseNaming checks the name before the symbol, BaseCalls
  rules out virtual calls before the speculative bind, NullCheck only builds the operation tree inside lambdas/queries).
  FieldNamingAnalyzer's TypeFacts no longer descend into trivia (that built every doc comment's XML; profiled with
  `dotnet-trace`, installed as a global tool) and skip the trivia walk without directives.
- `scripts/benchmark`: every run (and every build in compare mode) gets freshly parsed sources, so a per-tree cache
  or red nodes can't carry over (numbers before 2026-10-05 aren't comparable). `compare <base> <head>` alternates
  both builds in one process (order alternates, GC before each), fastest run per analyzer, up to two more rounds
  before a slowdown counts; fails at total >10 % and 25 ms, one analyzer >50 % and 15 ms, a new analyzer >60 ms.
  `STYLEBRO_BENCH_ONLY=Name,...` times only those. CI job `performance` runs it on every PR; a REQUIRED check.
  Nightly drift check (2026-10-22, owner, after the performance audit): `.github/workflows/drift.yml` (04:00 UTC +
  `workflow_dispatch`) runs `scripts/benchmark/Test-Drift.ps1`: main against the `StyleBroSelfVersion` package the
  self-check downloads, same thresholds; a failure opens or updates "Nightly drift check failed". Fix-time budget:
  `MaxFixSeconds` per repo in scripts/realworld/repos.psd1 (the `dotnet format` runs together), warn only
  (`::warning::` + job summary); provisional values, the owner sets ~2x the nightlies' median per runner, then it fails.
  Since 2026-10-09 CI compares with `--all-rules` (off-by-default rules measured too). The flag works (checked: 4,087
  -> 4,439 reports on Newtonsoft.Json); an earlier "totals match" was a misreading.
- The benchmark builds and times `TreeWalk`'s arrays before the analyzers, as their own line (2026-10-09): the first
  analyzer to touch a tree paid for the walk, mostly FieldNamingAnalyzer (the only symbol-action analyzer; symbol actions
  run before a file's tree/node actions), which swung 72-101 ms; now ~20 ms. `NamespaceNames.IsGenerated` is cached per
  tree (it built the file header's text per rename candidate). Remaining swings are GC pauses (`Monitor.Enter` in traces).

**StyleCop**: the open-bug sweep (54 open reports on replaced rules: 34 not shared, 8 shared and fixed or kept by
decision, 12 n/a; table in differences-from-stylecop.md), parity with StyleCop's unreleased `master` (31 changes,
gaps fixed; `Compare-WithStyleCop.ps1 -StyleCopPackage/-StyleCopVersion/-StyleCopFeed` runs the parity sets against a
locally built master), and its open PRs (checked 2026-10-05: nothing missing).
- BRO1104 writes `default(nint)` for `new nint()` everywhere (not `nint.Zero` on .NET 7+ like master): a choice that
  depends on the target framework gives a multi-targeted project's copies different fixes. Same trap as `#if`.
- BRO1505: two single-line properties may sit together (master); BRO1601: no `<inheritdoc/>` on explicit interface
  implementations (master), and its inserted doc comment brings BRO1513's blank line when the line above is code.
- BRO1501 keeps blank lines that group initializer entries (#2832); BRO1504: no blank line needed after `=>` (#3392).

**New rules** (ON in the preset and OFF after `bro migrate` unless noted; a rule StyleCop doesn't have is turned
off by migrate automatically: its description names no SA id. GOTCHA: an `SAxxxx` in a description makes
`Migration.ExpandIds` treat the rule as a StyleCop replacement):
- BRO1617 generic crefs in braces, BRO1618 `<see langword>` for a keyword alone in `<c>`, BRO1619 top-level doc
  element order (stable sort reusing BRO1611's slots); `Documentation/DocumentationStyle.cs`. BRO1504 option
  `stylebro_comment_blank_line_exempt_prefixes`.
- BRO1136 `(x) => x`, BRO1137 redundant trailing `return;`/`yield break;`, BRO1138 unneeded `$`/`@`/raw string,
  BRO1139 `else if`, BRO1140 empty record body, BRO1141 object creation parentheses
  (`stylebro_object_creation_parentheses`), BRO1142 one local per declaration (in CombinedFields), BRO1408 redundant
  base type. Shared fix base `NodeCodeFixProvider`, `Trivia.IsBlank`.
- BRO1526 switch-section blank lines (`stylebro_blank_line_between_switch_sections`, default include; defers to
  BRO1519 after a `}` it judges), BRO1143 no `else` after a jump (OFF by default; adds the brace rules' braces on the
  `if` branch so it converges in one run), BRO1001 `stylebro_keep_overloads_together` (default false).
- BRO1306 follows `dotnet_naming_rule.*` for private constants/static readonly fields; BRO1313 parameter names like
  the base (OFF); BRO1314 `Async` suffix (OFF; skips tests, controllers, hubs, attributed methods). The renamer is
  all-or-nothing across linked copies (a partial rename broke Polly's build) and skips members that also implement
  something it can't rename.
- BRO1409 `internal` instead of `public` on ordinary methods of internal types (OFF; the narrow variant of StyleCop
  #11: properties/fields/constructors stay, reflection, serializers, DI and test runners see public members only;
  many more skips, see the rule page; its fix sorts with BRO1001 in the same run).
- BRO1139 leaves an `else` after a jump to BRO1143 when that rule is on.

**`bro init --modernize`** (docs/modernizing.md): the SDK's modernization rules in tiers (A everywhere,
B needs LangVersion set in multi-targeted projects, C needs a newer API). `Modernize/MultiTargetSuppressor` hides tier
C in a project with 2+ frameworks when one lacks the API (table of minimum versions; maintenance: a new tier C rule
needs a row, `TheMultiTargetGuard_CoversExactlyTierC` enforces it). The package targets pass
`StyleBroTargetFrameworks` (`;` replaced by `,`: `CompilerVisibleProperty TargetFrameworks` breaks because `;` starts
an editorconfig comment). verify-package check 5 tests it end to end.

**Fix order** (lessons):
- `FixOrderTests` asserts that EVERY order gives identical TEXT, not only that no diagnostic is left: the BRO1139 /
  BRO1143 clash and a BRO1508/BRO1509 one (inner block expanded alone, `void M() { if (x)` left on a line nobody
  reports) both left no diagnostics. BRO1508/BRO1509's fix now also expands every enclosing single-line block whose
  rule is on, and adds BRO1519's blank line after a local function.
- `Severities.IsOn` without `enabledByDefault` uses the rule's own default from its descriptor (it treated every
  unconfigured rule as on, also BRO1143/BRO1310/..., wherever a fix only got an `isOn` callback).

**Gotchas from parallel agent work**:
- Real-world `-Work` folders: OUTSIDE the repo tree (inside, the repo's Directory.Build.props/Packages.props leak into
  the clones, which then fail to build and the script reports 0 findings as "all good") and unique per agent (the
  default folder hit another agent's DLL lock; scratchpad clones lost `.git`). `Invoke-RealWorld.ps1 -Enable <ids>`
  turns on off-by-default rules. Exception: `.trials/` (owner, 2026-10-15: one place to keep track of them) is
  gitignored and holds stub `.editorconfig` (`root = true`), `Directory.Build.props/.targets` and
  `Directory.Packages.props`, so clones there inherit nothing; first-run trials go to `.trials/trialNN/<repo>`, repros to
  `.trials/trialNN/spot/`, logs to `.trials/trialNN/logs/` (checked: a project there sees no StyleBro property or CPM). Since 2026-10-17 (owner: "Hybrid: cloud
  sessions for Linux repos, host for Windows-only, Docker as fallback") library repos and spot checks run as cloud
  sessions, Windows-only repos on the host under the no-desktop rule; `scripts/trials/` (Docker) is the fallback for local
  reruns and a future Windows container: `.trials/README.md`.
  Trial repositories are chosen ahead (owner, 2026-10-19): `scripts/trials/used.tsv` (everything used or rejected),
  `pool/` (one checked candidate per file, tagged), `random.json` (fixed, append-only random list); a trial adds its four to
  `used.tsv` in its PR.
- `$$` in unit-test markup is the caret position (raw `$$"""` strings are tested in samples/Messy only).
- Parallel rule PRs conflict in the same list files. Since 2026-10-11 the append-only lists are one file per entry
  (decisions, backlog items, mutation entries) and the rest are `merge=union` in the committed `.gitattributes`
  (Unshipped.md, DiagnosticIds.cs, the preset, rules/README.md, differences-from-stylecop.md, mkdocs.yml, survey psd1/csv,
  CLAUDE.md). GitHub's server-side merge IGNORES merge=union (tested with the merges API: 409), so a PR can still show a
  conflict: `git merge origin/main` locally, then check for resurrected or doubled lines (an old paragraph under a
  rewritten one, a row both sides edited) and same-named Messy samples (two `Uploader.cs` with a class `Uploader`).
  Keep the Unshipped.md `### New Rules` table even when empty, and rows in id order there and in the rule index
  (`DocExamplesTests.TheRuleListsAreInIdOrder`): rules of different areas then land in different places.

## Added 2026-10-05/06 (more real-world repos, C# 14, IDE speed)

Decisions in `docs/decisions.md`, details on the rule pages and in the PRs (#48-#56).

**New rules** (beyond StyleCop; off after `bro migrate`):
- BRO1527 auto-accessors on one line (on; skips bodies, attributes, comments, `csharp_preserve_single_line_blocks =
  false`), BRO1145 empty class/struct/interface body -> `;` (OFF; C# 12+, multi-target LangVersion guard), BRO1146
  `record class` -> `record` (#52). BRO1505 and BRO1001 judge a property BRO1527 collapses as one-line
  (`AutoAccessorLines.BecomesOneLine`), so the sort and the collapse give the same text in every order (#55).
- BRO1144 (on) escapes identifiers C# 14 reads as keywords (`field` in accessors, `extension`, `partial`) with `@`;
  BRO1001 sorts extension blocks right before the methods (owner: "With methods, first"), BRO1509 expands one-line
  ones (#53). StyleBro builds against Roslyn 4.8, which has no C# 14 nodes: `CSharp14.cs` detects them by runtime type
  name, unit tests can't parse C# 14, so samples/Messy (net10.0) covers it. A renamed member named `field` takes the
  keyword uses along (checked 2026-10-06 with a throwaway C# 14 project; Messy can't show it: BRO1303's string guard
  sees "field" in the referenced StyleBro projects).
- BRO1147 redundant `!` and BRO1148 `HasValue` -> null check (both OFF, #54). Not IDE0370 in init (owner).
- Never rename to `field` (C# 14 keyword) or to `value` inside a property/indexer/event (`CamelCaseNames.IsUsableName`);
  IDE0031 moved to modernize tier B (writes `x?.P = v`, C# 14), IDE0350 added there (#49).
- BRO1313 keeps a parameter whose name reaches run time (`nameof`, `[CallerArgumentExpression]`: ParamName changed in
  Newtonsoft.Json); BRO1302 keeps renaming such names (owner, like SA1313; #56).

**Multi-target/LangVersion guards**: the package targets pass `StyleBroTargetFrameworks`, `LangVersion` and
`MaxSupportedLangVersion` as compiler-visible properties; `LinkedFileFixAllProvider.GetLowestLanguageVersion` lets a
fix write only what every copy's language version accepts. Without the package targets (a bare `<Analyzer>`), the
guards can't see the frameworks: documented. Invoke-RealWorld's hook passes the same properties.

**Real world** (#51): eShopOnWeb (Razor/Blazor), MediatR (open generics), LibGit2Sharp (P/Invoke, net472+net8.0),
Mapperly (source generator, newest C#), all REQUIRED checks now (16 required in total). Found: renames broke Razor
pages (a type with a generated part isn't renamed, `NamespaceNames.HasGeneratedPart`; the fix drops a rename with a
reference in generated code). `-Enable A,B` from another shell was ONE string and enabled nothing before #51 (now
split on commas); confirm an -Enable run really reports the rule. `TestFilter` in repos.psd1 leaves out tests that
need the network (LibGit2Sharp's fetch/clone/push failed on CI only).

**Performance** (#50, owner asked what the IDE pays per edit): `scripts/benchmark file <dll> <folder> [N | a.cs;b.cs]
[runs] [symbols] [--all-rules]` analyzes each file in its own compilation (the IDE's view), semantic model bound
first and not counted. Fixed: ElseIfAnalyzer re-parsed per `if` (quadratic), plus several analyzers that scaled
worse than the file; now time grows with file size (8x size -> 8.4x time). Jellyfin EncodingHelper.cs 170 -> 99 ms,
Newtonsoft XmlNodeConverter 58 -> 24 ms. Whole project (Newtonsoft.Json, 72 analyzers, 2026-10-06): ~485 ms; StyleCop
1.2 is 3.1-3.4x slower (docs/stylebro-vs-stylecop.md, #48).

**Performance round 3** (2026-10-07, owner: "performance is an important metric for analyzers"): Newtonsoft.Json
354-371 -> 226-254 ms (-31 to -36 %, compare mode), same reports per analyzer on four sources. `TreeWalk.Trivia` is now
a cached array WITHOUT whitespace and line breaks (every caller wanted only comments/docs/directives); tokens and
nodes come from one walk; directives are found in that array (not `GetNextDirective`); EmbeddedCommentAnalyzer starts
from the comments; cheap layout checks before trivia/previous-token lookups (ParameterLayout, ListGaps,
ParenthesisPlacement, BlankLineRuns); FieldNaming matches an old name only in strings containing it (no word split).
Lessons: `dotnet-trace` samples land on safepoints (`PollGC`, `Monitor.Enter`), so a hot method in the profile can be
noise (FieldStyles' reflection hash: called ~250 times); the shared walk lands on whichever analyzer runs first per
file (FieldNamingAnalyzer swings 35-90 ms between runs), so judge by the total and by analyzers that don't pay it.

**Lessons**:
- A keep-both merge resolution can cut through a raw string when the shared lines sit outside the conflict: build
  before trusting mutation results (a compile error "kills" every mutation). After a merge, also check
  that every mutation entry's Find text still exists (a refactor leaves stale entries).
- Check that a "two runs" note is really fix order: the `field` case was a guard keeping the rename back.

## Added 2026-10-06/08 (release, first-run fixes, options, speed)

- **0.2.0-alpha.1 released** (2026-10-06, owner's call): 124 rules shipped (AnalyzerReleases.Shipped "Release 0.2.0.1"),
  self-check moved to it (#62; the preset now applies to StyleBro's own code: usings sorted). Released versions:
  update the "Status" section above when the next one ships.
- **First-run trial** (local packages, 4 new repos, docs/backlog.md): BRO1601 skips members with `#if` above them and
  projects without doc generation (`build_property.GenerateDocumentationFile`, compiler-visible via the package
  targets); migrate pins StyleCop's casing (`stylebro_private_static_field_naming`), skips git submodules, merges
  `format` reports. README's rule table moved to `docs/rules/README.md` (index, Default column; a test keeps it
  complete; New-Backlog.py reads it); contributor docs in CONTRIBUTING.md.
- **Options** (decisions.md 2026-10-07: .editorconfig only, never a stylebro.json; defaults = StyleCop's behavior, the
  preset sets none): eleven keys, listed in that entry and on the rule pages. Lesson from merging them: options of
  different rules interact (#67's same_line joins asked BRO1110's default rule; with #68's own_line that pulled a `)`
  off its own line). Ask the other rule's CONFIGURED decision (`ParenthesisPlacement.MovesCloseToLastItem`), and add a
  FixOrderTests case with both options on.
- **Speed round 3** (#63): `TreeWalk.Trivia` holds only comment/directive trivia; tokens and nodes in one walk; about a
  third off the total. dotnet-trace blames whatever it samples during thread suspension: confirm with `compare`.
- **CI time:** the mutation check took 70-106 minutes in one job; since 2026-10-09 (#71) it runs as 4 shards (13-18
  minutes each measured, timeout 60) plus the summary job `mutation` (`if: always()`, fails unless `needs.shard.result` is
  success). Give new entries a NARROW `Tests` filter (a FixOrderTests case with 7 ids is 5,040 orders, ~10 minutes).
- **Paused briefly** (2026-10-08) and resumed the next day (owner: "let's ignore my previous hesitations"); open
  ideas are the "Idea" rows in docs/backlog.md.

## Added 2026-10-09/10 (0.3.0 and 0.4.0 releases, Sonar, trials 2 and 3, docs site, CI speed)

Decisions in `docs/decisions.md`; details on the rule pages and in PRs #71-#97.

**Releases**: 0.3.0-alpha.1 (2026-10-09) and 0.4.0-alpha.1 (2026-10-10), both by the owner's call. Release notes are
written by hand (`gh release edit --notes-file`) instead of GitHub's PR list. Since 2026-10-11 they come from
changes/ fragments folded into CHANGELOG.md by the release PR, and merging it releases (see release.yml above).

**Docs site** (#82): MkDocs Material, `mkdocs.yml` + `.github/docs-requirements.txt`, deployed to GitHub Pages
(https://bisforboman.github.io/stylebro/) from main by `.github/workflows/docs.yml`; PRs build `--strict`. Every rule's
help link is `https://bisforboman.github.io/stylebro/rules/BROxxxx/` (`Descriptors.HelpBase`; fading companions like
`BRO1405_p` link to their rule's page). A new rule page needs a row in `docs/rules/README.md` AND an entry in the
`mkdocs.yml` nav (DocExamplesTests checks both); new `stylebro_*` keys need a row in `docs/configuration.md` (a test
checks). Internal docs (decisions, backlog, surveys, proposals) are excluded from the site; link to them with absolute
GitHub URLs.

**CI** (#71, #77, #87): mutation runs in 4 shards + a summary job named `mutation` (the required check); a `changes`
job per workflow lets docs-only PRs pass the heavy jobs in seconds (skips are on STEPS, so matrix check names still
appear); on PRs only the mutation entries for changed files (and their test files) run, the full set on main. The
performance check compares with `--all-rules`. Real-world jobs are the queue bottleneck (12 Windows jobs per PR, ~20
concurrent jobs on the free plan): avoid many parallel PRs that touch src/.
Since 2026-10-10 (owner's decision): real-world and mutation don't run on push to main, only on PRs and NIGHTLY on main
(02:00/03:00 UTC + `workflow_dispatch`); a failed nightly opens or comments on the issue "Nightly real-world run failed"
/ "Nightly mutation run failed" (`.github/nightly-issue.sh`). ci.yml (build, parity, performance) still runs on push.
Real-world jobs cache the clone's `.git` (`realworld-clone-<repo>-<commit>`) and `NUGET_PACKAGES`
(`realworld-nuget-<os>-<repo>-<commit>-sdk<ver>`); saved only outside PRs (main's caches reach every PR; PR caches
would evict them from the 10 GB limit), so a new pin is cached by the next nightly. The script skips the fetch when the
commit is already in the clone; StyleBro's DLLs are never in a cached path.

**Sonar** (#89, #90, #94): `bro migrate` reads Sonar setups (`SonarSetup.cs`: exported profile via
`--sonar-profile`, the package's default "Sonar way" list from `data/`, rulesets incl. the scanner's, globalconfigs,
root .editorconfig) and maps Sonar ids to StyleBro/SDK rules (`data/sonar-mapping.tsv`; public code excluded where a
fix would change an API); Sonar suppressions carry over like StyleCop's. BRO1149 (nested ifs, S1066), BRO1150
(`Where(p).Count()`, S2971), BRO1151 (params array, S3878) are our own rules written from Sonar's public rule
descriptions (never read or port Sonar's source: its license has a "competing" clause); their fix providers also list
S1066/S2971/S3878 in FixableDiagnosticIds, so `dotnet format` fixes Sonar's own warnings where our logic accepts the
node (`NodeCodeFixProvider` second id). Tests use a stub analyzer at Sonar's locations; `scripts/sonar-interop.ps1`
(weekly workflow) runs the real pinned package. Sonar is never a dependency of StyleBro's packages.

**Rules and guards added after the trials** (trial 2 #76/#80, trial 3 #91-#93):
- BRO1008 replaces IDE0065 (SA1200; IDE0065's binding change reported as https://github.com/dotnet/roslyn/issues/86002): moves usings only when C#'s lookup order shows every name binds the same (no
  rebinding: 180 ms -> 5-9 ms); `#region` lines don't block it; UNSET `csharp_using_directive_placement` = nothing
  reported (owner); the preset says outside, migrate writes StyleCop's choice, init detects the repo's majority.
- BRO1410 parentheses in patterns + hidden `_p` fading companions (NotConfigurable, not in DiagnosticIds; tests
  filter them unless `IncludeFades`).
- Naming: `nameof(x)` used as a value blocks renaming x (solution-wide for members); `stylebro_rename_public_api`
  (default false: names visible outside the assembly aren't renamed; parity sets turn it on); snake_case names aren't
  BRO1309's; a DTO's properties are renamed all or none.
- Comments: a `//` run right below a statement, followed by a blank line, belongs to the code above (BRO1504/BRO1506
  leave it, BRO1001 skips types whose sort would separate them); group comments keep their group.
- BRO1604 only prefixes noun phrases; BRO1601 skips projects without doc generation and members with `#if` above;
  findings in NuGet package folders (contentFiles) are suppressed (`PackageFileSuppressor`).
- Source generator output (trial 12, RestSharp: ~950 warnings in a header-less generated file): Roslyn 4.8 counts a file as
  generated only by name/header (`GeneratedCodeAnalysisFlags.None` doesn't help) and has no public marker for generator
  trees. Probed: the compiler names them `<IntermediateOutputPath>/<generator assembly>/<generator type>/<hint>` (under
  `generated/` or `CompilerGeneratedFilesOutputPath` with EmitCompilerGeneratedFiles). `GeneratedFileSuppressor` hides BRO
  findings under the folders the package targets pass (`StyleBroGeneratedFolders`: obj/ + the emit path), else under an
  `obj` folder. `GeneratedFileSuppressorTests` runs every analyzer on such a file.
- `init` (Conventions.cs) detects 20 conventions (incl. `_` fields and using placement) and writes the majority at
  >= 75 % of >= 10 places; stops for StyleCop repos (use `--write`); `format` limits fixes to BRO + init's IDE/CA ids
  (`--all` for everything) and repeats until clean; `--diff` previews init / migrate / format on a temp copy.

**Lessons**:
- Every trial on unseen repos found real bugs (loops, build breaks, behaviour changes): keep doing them after
  releases; run the repos' TESTS, not just builds (Ocelot's header value changed through `nameof`).
- Options of different rules interact: ask the other rule's CONFIGURED decision, and add a FixOrderTests case with both.
- After merging main into a PR, re-run the tests AND check mutation entries still match: refactors leave stale or
  surviving entries (#91, #92); test edits can stop exercising a guard.
- A "conflict" right after a push is often GitHub recomputing: watchers should require DIRTY several times.
- Agents squashing onto a moved main can revert other PRs' changes: check a PR's file list before trusting it.
