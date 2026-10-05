# Decisions

Design questions that came up while building StyleBro, the choices considered, and what was decided. Newest first.

## Multi-target guard for the newer-API rules (2026-10-05)

### Question

`init --modernize` wrote tier C (the SDK rules whose fixes need a newer API) as `suggestion` in any repository with a
multi-targeted project, because the rules report in the newer framework's compilation and `dotnet format` writes the
fix into the file the older framework compiles too. Build the guard suppressor the 2026-10-04 decision left on the
backlog?

### Choices

1. Build it: a `DiagnosticSuppressor` that knows all of a project's target frameworks and hides a tier C diagnostic
   when one of them lacks the API; tier C becomes `warning` everywhere.
2. Keep the `suggestion` fallback.

### Decision

The owner: build it ([modernizing.md](modernizing.md#the-multi-target-guard)). Agent's choices: the package passes
`StyleBroTargetFrameworks` (`TargetFrameworks` with `,` for `;`, since `;` starts a comment in the generated
editorconfig); a hand-kept table of minimum .NET (Core) / .NET Standard versions per rule, set to the newest API the fix
can write (CA1872 .NET 9 for `ToHexStringLower`, CA1850 .NET 6 for HMAC, CA2263 .NET 5); unknown frameworks count as
lacking everything; single-target projects are never touched. `init --modernize` writes tier C at `warning` always, no
`suggestion` fallback: the guard ships in the same package as the preset `init` sets up, and a note says projects
without the package have no guard. A MigrationTests check keeps the table and tier C in sync.

## Switch section blank lines, no `else` after a jump, overloads together (2026-10-05)

### Question

Three candidates from [beyond-stylecop.md](beyond-stylecop.md) (#6, #16, #17): Roslynator's RCS0061 (blank line between
switch sections), Meziantou's MA0071 / Roslynator's RCS1211 (no `else` after a branch that ends in a jump) and Sonar's
S4136 (overloads next to each other). Defaults, and how each agrees with the rules that touch the same code: BRO1519
(SA1513) wants a blank line after a case block's `}` before the next label (kept in "StyleCop's open bugs", #3568), and
BRO1001 sorts by StyleCop's access order.

### Choices

- RCS0061: `include` / `omit` / `omit_after_block`, on or off in the preset; what to do where BRO1519 wants the opposite.
- MA0071: on or off; `else if` chains (whole chain, or skip); how much to recognize as "ends in a jump".
- S4136: a new rule or a BRO1001 option; which members count as overloads.

### Decision

- **BRO1526** (owner): option `stylebro_blank_line_between_switch_sections = include | omit | omit_after_block`, default
  `include`, on in the preset, off after `stylebro-migrate`. Agent's resolution of the conflict: after a section whose
  `}` BRO1519 judges (a multi-line block or if/else at the end), BRO1526 reports nothing when BRO1519 is on, so the
  two never undo each other; with `omit`, a blank line stays there unless BRO1519 is turned off (documented). A
  one-line block isn't BRO1519's and follows the setting. Comments and directives between sections are skipped; switch
  expression arms aren't sections.
- **BRO1143** (owner): off by default (`isEnabledByDefault: false`, preset `none`), it's a matter of taste. Agent's
  choices: `else if` chains skipped entirely (removing the first `else` would only make a new candidate); "ends in a
  jump" is syntactic (a jump statement, or a block whose last statement is one); skipped when a name declared in the
  body is used in the enclosing block, for `using` declarations, comments around the `else`, `#if`, and lines that
  can't simply be moved out (multi-line strings and comments, unusual indentation). An `if` branch without braces next
  to an `else` block is BRO1516's first (the chain is inconsistent; without the `else` it would become BRO1514's, which
  may already have run), so the `else` goes on the next run.
- **BRO1001 option** (owner): `stylebro_keep_overloads_together = false | true`, default `false` (StyleCop's order).
  Agent's choices: methods only (constructors and indexers are kinds of their own, so always together; operators
  aren't grouped by symbol, so `==`/`!=` pairs stay); a group sorts at the place of its overload that sorts first, the
  other overloads right behind it in their own order; within a region; explicit interface implementations group among
  themselves.
## Readability batch B: eight rules beyond StyleCop (2026-10-05)

### Question

Which of these candidates (StyleCop issues #762 and #760, and survey #7, #8, #10, #12, #13 and #14 in
[beyond-stylecop.md](beyond-stylecop.md)) should StyleBro build, and how should they be configured: `(x) => x`,
redundant `return;`/`yield break;`, unneeded `$`/`@`/raw strings, `else if`, empty record bodies, object creation
parentheses, one local per declaration, redundant base types?

### Choices

1. Build them all, on in the preset (`warning`), off after `stylebro-migrate` like the other rules beyond StyleCop.
2. Build them off by default (opt-in, like BRO1310).
## Naming batch: camelCase constants, base parameter names, Async suffix (2026-10-05)

### Question

Three naming candidates from [beyond-stylecop.md](beyond-stylecop.md): camelCase private constants and static readonly
fields (StyleCop #2641, #3793; tracker #10), a parameter named like the one it overrides or implements (StyleCop's
proposed SA1315, #1949, the SDK's CA1725 without a fix; tracker #12), and the `Async` suffix on asynchronous methods
(Roslynator RCS1046, Meziantou MA0137; survey #18). All three are renames, so the rename guards apply, and two of them
change names other code depends on (named arguments, public method names).

### Choices

1. camelCase constants: a new rule; an option for BRO1306 (a `stylebro_*` key, or following the SDK's
   `dotnet_naming_rule.*` like BRO1303 does); skip.
2. Base parameter names: on by default; off by default; skip (CA1725 reports it).
3. `Async` suffix: on by default; off by default; skip (renames public API, frameworks bind some names).

### Decision

The owner: BRO1306 follows the SDK's naming rules, no new id: a naming rule that singles out private constants
(`required_modifiers` with `const`) or private static readonly fields (`static`/`readonly`) and asks for camel case
(no prefix or `_`) makes BRO1306 rename them that way; without one, PascalCase like StyleCop. BRO1313 (base parameter
names) and BRO1314 (`Async` suffix) are built and off by default (`stylebro-migrate` turns them off too; StyleCop never
shipped either). BRO1313 matches bases in libraries too and skips discards, disagreeing bases, partial methods and
clashing names; BRO1314 counts `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` and `IAsyncEnumerable<T>` return types
and skips `Main`, names containing `Async`, attributed methods (tests and frameworks), controllers, hubs and event
handlers. The reverse (no `Async` on synchronous methods) is left out.
## Documentation batch A: crefs, langword, element order, tool markers (2026-10-05)

### Question

Four candidates from [beyond-stylecop.md](beyond-stylecop.md) (StyleCop issues #758 and #3546/#1490, Meziantou
MA0154, survey #15): should they be built, on by default, and what does `stylebro-migrate` do with them?

### Choices

1. All on in the preset, off after `stylebro-migrate` (like the other rules StyleCop doesn't have).
2. Off by default (opt-in, like BRO1310/BRO1312).
3. Leave them in the backlog.

### Decision

The owner: **choice 1**, all eight, as BRO1136-BRO1142 and BRO1408. Object creation parentheses get
`stylebro_object_creation_parentheses = omit | include` with `omit` as the default; target-typed `new() { }` keeps its
parentheses. One rule for the three string forms (BRO1138). BRO1408 covers `enum E : int` and `class C : object` only.
The owner: build all four, on in the preset and off after `stylebro-migrate`. BRO1617 (`cref="List{T}"`), BRO1618
(`<see langword="null"/>` for a keyword alone in `<c>`; reserved keywords plus MA0154's contextual ones, kept
conservative), BRO1619 (top-level elements in the order summary, typeparam, param, returns, value, exception, remarks,
example, seealso; Roslynator's RCS1232 turned out to cover `<param>` order only, so the order was picked from Visual
Studio's `///` stub and Microsoft Learn's API pages, see the rule page). The BRO1504 option is
`stylebro_comment_blank_line_exempt_prefixes`, default empty, so BRO1504 behaves like SA1515 until a team sets it.

## Modernizing code for newer runtimes (2026-10-04)

### Question

Can StyleBro make code "nicer for newer runtimes" (target-typed `new`, collection expressions, `ThrowIfNull`, ranges,
`System.Threading.Lock`, ...)? A survey found a Fix All fixer in the SDK for every important rewrite, so no StyleBro
rule is needed. The SDK lacks one thing: in a multi-targeted project the rules fire per target framework and
`dotnet format` writes the newer framework's edit into the shared file. Probed: 14 of 15 API rules broke the net48
build (CA1847 silently bound `Contains(char)` to LINQ there), and 15 of 18 language rules broke it unless LangVersion
was set explicitly.

### Choices

1. `stylebro-migrate init --modernize`: an opt-in block of the SDK's rules in tiers, at `suggestion` where they aren't
   safe in the repository's multi-targeted projects.
2. A multi-target guard: a `DiagnosticSuppressor` that knows all of a project's target frameworks and hides the API
   rules an older one lacks, so every tier works in multi-targeted repositories.
3. Backlog only.

### Decision

The owner: `init --modernize` now ([modernizing.md](modernizing.md)); the guard suppressor not now (backlog, Maybe).
The tiers: older C# (A) at warning everywhere; newer C# (B) at warning unless a multi-targeted project leaves
LangVersion unset; newer APIs (C) at warning only without multi-targeted projects. Debated rules (IDE0290, IDE0066,
IDE0063, IDE0305) and IDE0251 are left to the user; rules without a `dotnet format` fix are skipped.

## StyleCop's open bugs that StyleBro shares (2026-10-04)

### Question

The sweep of StyleCop's open bug reports (results in
[differences-from-stylecop.md](differences-from-stylecop.md#stylecops-open-bugs)) found 8 that StyleBro shares. Five are
plain bugs and were fixed. Three are StyleCop's behavior that its users call a bug: should StyleBro keep matching
StyleCop there?

- [#3392](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3392) (BRO1504): a comment right after `=>` (a
  switch expression arm, also lambdas and expression bodies, #3550) gets a blank line between the arrow and the comment.
- [#2832](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2832) (BRO1501): a blank line grouping the
  `{ k, v }` entries of a dictionary initializer is removed.
- [#3568](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3568) (BRO1519): a blank line is wanted between a
  case block's `}` and the next `case` label.

### Choices

For each: change StyleBro (and say so under "Compared with StyleCop"), or keep StyleCop's behavior.

### Decision

The owner: #3392 changed, `=>` counts like `{` for BRO1504 (this reverses the earlier rejection of #3550 in
[beyond-stylecop.md](beyond-stylecop.md)). #2832 changed too, after seeing code examples: BRO1501 leaves the brace of
an initializer's entry alone, so blank lines grouping the entries stay (first answer was "keep"). #3568: keep
StyleCop's behavior (BRO1519 wants the blank line between a case block's `}` and the next label).
## Parity with StyleCop's master (2026-10-04)

### Question

StyleCop's unreleased `master` has ~60 commits since 1.2.0-beta.556 that change rules StyleBro replaces (a parity run
against a locally built `master` package confirmed them). Follow `master` everywhere, or only where it fixes a gap?

### Choices

For each change: follow `master`, or keep StyleBro's (beta.556-like) behavior and document the difference. The clear
gaps: BRO1104 `new nint()` (beta's `nint.Zero` doesn't compile before .NET 7), BRO1604/BRO1605 `init` accessors (and
the `Gets or sets or initializes` fix), BRO1611 primary constructor `<param>` tags, BRO1401 property patterns; also
BRO1606/BRO1607 missed multi-line whitespace-only summaries, which StyleCop reports. Changes that make `master` report
less: BRO1505 two single-line properties without a blank line (2aeb4e3d), BRO1601 explicit interface implementations (2959cac8), BRO1606 any summary starting
with `<para>` accepted without checking its text (29514d23); BRO1311 skipping overrides/implementations (d1b6183d) was
already a documented difference.

### Decision

The owner: the clear gaps follow `master`; StyleBro keeps its behavior for BRO1606 (`<para>` text checked), documented
on the rule pages and in [differences-from-stylecop.md](differences-from-stylecop.md). First also kept for BRO1505 and
BRO1601; after seeing code examples the owner changed both to follow `master` (same day): two single-line properties
may sit together, and explicit interface implementations get no `<inheritdoc/>`. The parity sets stay on beta.556;
`Compare-WithStyleCop.ps1 -StyleCopFeed` runs them against a local `master` build when needed (no CI job).

## Superseding StyleCop, and performance (2026-10-04)

### Question

StyleCop parity is done (every rule StyleBro, SDK or dropped with a reason). To properly supersede StyleCop, what else
goes on the backlog? And after the analyzer speed-ups (all 55 analyzers ~830 -> ~700 ms on Newtonsoft.Json), what
performance work?

### Choices

Backlog: (1) a sweep of StyleCop's open bug reports on the rules StyleBro replaces, each checked with a test; (2) the
remaining candidates from StyleCop's tracker (beyond-stylecop.md #6-#10, #12); (3) parity with StyleCop's unreleased
`master` (~20 commits that change what a replaced rule reports); (4) the remaining candidates from other analyzers
(survey #6-#10, #12-#18). Performance: a CI regression check, a FieldNamingAnalyzer deep dive (with a profiler), one
shared token pass for the layout analyzers, a per-file IDE responsiveness check.

### Decision

The owner: all four backlog items ("to properly supersede StyleCop we should have implemented most of their reported
issues/planned improvements too"), and the CI check, the FieldNamingAnalyzer deep dive and the shared token pass
("performance is an important metric for analyzers, you don't want them to be slow"). Not now: the per-file IDE check.
All are under Work in [backlog.md](backlog.md).

## Comments in declaration headers (StyleCop issue #605) (2026-10-04)

### Question

StyleCop issue #605 ([beyond-stylecop.md](beyond-stylecop.md#from-stylecops-issue-tracker) #4) proposes SA1108's check
for declarations: no comment between a class's, namespace's, member's or accessor's header and its `{`. Extend BRO1132
(same id) or a new rule, and which declarations?

Survey of the eight reference repositories (scripts/realworld/repos.psd1), 50,403 declarations with a `{` (7,989 types,
2,462 namespaces, 29,151 methods/constructors/operators, 9,864 properties/indexers/events, 734 accessors, 203 local
functions): 9 with a comment between the header and `{`. 7 single-line headers with a trailing comment (methods: CsvHelper
1, Jellyfin 1, Polly 1, Serilog 1, mostly a note or issue link after a test name; types: Newtonsoft.Json 3, e.g.
`class ErrorPerson2 //:IPerson - oops!`), and 2 multi-line headers (Jellyfin's comment after the last parameter, Serilog's
ReSharper markers around a constructor initializer), which the BRO1132 rule for multi-line headers (decision A' below)
skips. None on accessors, property accessor lists, namespaces or local functions.

### Choices

1. **A new id, BRO1134**, sharing BRO1132's code (same analyzer and fix, same skips): `stylebro-migrate` maps BRO1132 to
   SA1108 and turns it on for repositories with SA1108 on, which shouldn't start changing declarations StyleCop never
   checked; teams can turn the two on separately.
2. **Extend BRO1132:** one id, but SA1108 then means more in StyleBro than in StyleCop, and migrated repositories change.

Scope: types (incl. enums, records with a body), block-scoped namespaces, methods/constructors/finalizers/operators with a
block body, a property's, indexer's or event's accessor list, accessor bodies, local functions. Comments after an
attribute (`[Fact] // flaky`) aren't in the header. Found on the way, fixed for both rules: with blank lines right after
`{`, the moved comment went above them, and a run that had already applied BRO1506 ("no blank line below a comment") left
one behind; it now goes below them, where BRO1503 removes them (FixOrderTests).

Preset: warning; `stylebro-migrate` writes it as `none`.
## Call chain layout (BRO1523, Roslynator RCS0054) (2026-10-04)

### Question

[beyond-stylecop.md](beyond-stylecop.md) #4: when a call chain is split over several lines, every call starts its own
line. Which chains count, what may stay on the chain's first line, what is a "call", and which indentation do new lines
get?

Facts: Roslynator's [RCS0054](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS0054) (off by default;
[analyzer](https://github.com/dotnet/roslynator/blob/main/src/Formatting.Analyzers/CSharp/FixFormattingOfCallChainAnalyzer.cs),
[fix](https://github.com/dotnet/roslynator/blob/main/src/Formatting.Analyzers.CodeFixes/CSharp/CodeFixHelpers.cs))
starts at the outermost invocation, element access or conditional access and walks every `.` and `?.` back from the
end. Each must start a line indented one unit deeper than the chain's first line, until it reaches one on the chain's
first line; so the first line may hold anything, a property step is split too (`.WriteTo` + newline + `.Sink(x)`), and
a chain is reported as soon as any `.` is off its first line, also when only the arguments span lines
(`Task.Run(() => { ... }).ConfigureAwait(false)`). Its fix re-indents the lines inside a moved step (lambda bodies).

Survey of the eight reference repositories (scripts/realworld/repos.psd1, default branches, 2026-10-04), chains with two
or more `.`/`?.` and at least one call, outside interpolated strings: 42,705; split (a `.` or `?.` starts a line):
20,959 (Jellyfin 17,215, Polly 2,179, OpenTelemetry 922, FFMpegCore 248, FluentValidation 189, Serilog 182, CsvHelper 13,
Newtonsoft.Json 11).

- First line of the split chains: the receiver only (`people` + newline + `.Where(...)`) 2,652; the receiver and one
  call (`services.AddA()` + newline + `.AddB()`) 17,800; two or more calls 58 (mostly FluentValidation's
  `RuleFor(x => x.Name).NotNull()` + newline + `.WithMessage(...)`).
- Already one call per line below the first line (this rule as chosen): 20,880 (99.6%); 79 chains, 110 calls, 45 files
  would change (Jellyfin 49 calls, FluentValidation 22, CsvHelper 12, Polly 12, OpenTelemetry 10, Newtonsoft.Json 4,
  Serilog 1, FFMpegCore 0). Typical: `.Cast<T>().Single()`, `.GreaterThan(100).WithName("Foo")`, CsvHelper's
  `.Map(m => m.A).Name("A1").Default("WEW")`.
- RCS0054's view (every `.` off the first line starts a line): 711 split chains differ, mostly property steps (Serilog
  142 of 182: `.WriteTo.Sink(...)`, `.MinimumLevel.Debug()`); plus 404 chains that aren't split but have a `.` after
  multi-line arguments.
- A property after the last call (`.OutputToFile(...).Arguments`, `.Result`): 46 chains keep it on the call's line
  (FFMpegCore 38).
- Indentation of the lines that start with `.`, against the chain's first line (20,510 chains whose breaks all sit
  before a call step): one unit (4 columns) 19,887; aligned elsewhere, usually under the first `.` 516 (Polly 443,
  OpenTelemetry 46); 0 columns 55; 8 columns 27; 2 columns 12; mixed within the chain 13.
- No call that would move has a comment or a directive in its gap; 2 multi-line steps have lines less indented than
  their new line would be.

### Choices

1. **Which chains:** only split chains (a `.`/`?.` starts a line), like BRO1108 for lists (never split a one-line chain);
   or also chains where only the arguments span lines, like RCS0054 (404 more chains, e.g. every
   `Task.Run(() => { ... }).ConfigureAwait(false)` would be split).
2. **The first line:** free, any number of calls (RCS0054); or the receiver and at most one call (50 more chains, mostly
   FluentValidation tests; and `RuleFor(x).NotNull()` would be fine while `validator.RuleFor(x).NotNull()` wouldn't).
3. **What starts a line:** every `.` (RCS0054: Serilog's `.WriteTo` + newline + `.Sink(x)`); or each call step: the members
   from the one after a call up to and including the next call (`.WriteTo.Sink(x)` stays together), with a property
   after the last call left alone.
4. **Indentation:** enforce one unit deeper than the chain's first line from `.editorconfig` (RCS0054; Polly's 443
   aligned chains would be re-indented, and moved steps with lambda bodies need re-indenting); or don't check it and give
   new lines the indentation of the chain's own lines that already start with `.`.

### Answer

Defaults chosen from the survey (agent's proposal, 2026-10-04, for the owner to review): split chains only; the first
line free; call steps (a member that only leads to the next call stays with it, a trailing property stays on its call's
line); indentation not checked, new lines copy the chain's own. That reports 110 calls in 79 chains across the eight
repositories, each a call tacked onto a line where another call already sits. Only the gap before the `.` is rewritten;
a multi-line step moves only when its other lines are at least as deep as its new line, so argument contents never
change. Skipped: comments in the gap, directives anywhere in the chain, interpolated strings, syntax errors. Warning in
the preset; off after `stylebro-migrate` (no StyleCop rule asks for it). An indentation option (choice 4, RCS0054's)
could come later as a separate setting if someone asks.
## No blank line after attributes (StyleCop's proposed SA1521) (2026-10-04)

### Question

BRO1525 ([beyond-stylecop.md](beyond-stylecop.md#from-stylecops-issue-tracker) #2): StyleCop's issue #738 proposed SA1521,
"attributes defined for type or member should never be followed by blank line", and never implemented it (no
documentation page, no analyzer). Which gaps count (before the element only, or also between two attribute lists of
one element), what about comments in the gap, and does it conflict with BRO1505 (elements separated by a blank line)?

Survey of the eight reference repositories (scripts/realworld/repos.psd1), 25,160 attribute lists on members, 36 on
accessors, 2,539 on parameters:

| Gap after an attribute list | Count | Repositories |
|---|---|---|
| Blank line, then the element | 8 | Jellyfin 5, Newtonsoft.Json 1, OpenTelemetry 1, Polly 1 (mostly `[InlineData]` rows above a test method) |
| Blank line, then the next attribute list | 1 | Jellyfin |
| Blank line and a comment, then the next attribute list | 9 | Jellyfin (comments grouping `[InlineData]` rows) |
| Blank line and a comment, then the element | 1 | Jellyfin |
| On accessors or parameters | 0 | |

BRO1505 doesn't count these blank lines (an element's code starts at its first attribute), so a member whose only blank
line sits below its attribute is already a BRO1505 finding; the two fixes edit different lines and converge in either
order (FixOrderTests).

### Choices

1. **Every attribute list except assembly/module ones, before the element and between lists; gaps with a comment,
   documentation comment or directive skipped** (BRO1504 wants a blank line above a comment that follows code; an `#if`
   changes which element the attribute belongs to). The 9 commented `[InlineData]` groups stay.
2. **Only before the element:** stacked lists may be grouped with blank lines; 1 finding in the survey.
3. **Also comments:** remove the blank line above the comment; conflicts with BRO1504.

Preset: warning; `stylebro-migrate` writes it as `none` (StyleCop never shipped SA1521, so a StyleCop-clean repository
may have such blank lines).
## Null check style: `x == null` or `x is null` (2026-10-04)

### Question

[BRO1133](rules/BRO1133.md) ([beyond-stylecop.md](beyond-stylecop.md) #3, Roslynator RCS1248) writes null checks in one
form. Which form by default, and what about `null == x`?

Facts: the SDK has no rule or option for it. `dotnet_style_prefer_is_null_check_over_reference_equality_method` only
covers `ReferenceEquals(x, null)` (IDE0041). Probed with SDK 10.0.401 (`dotnet format style` and `analyzers`,
IDE0041/IDE0150/IDE0083/IDE0078 at warning with their options on): `x == null`, `x != null`, `null == x` and
`n == null` (an `int?`) stayed as they were; only `ReferenceEquals(o, null)`, `!(o is null)`, `o is object` and
`s == null || s == ""` were rewritten, all into patterns. Nothing turns `x is null` into `x == null`.

Survey of the eight reference repositories (scripts/realworld/repos.psd1, text count of `*.cs` lines that aren't
comments; `is null` also counts a few strings such as messages):

| Repository | `== null` | `!= null` | `null ==` | `is null` | `is not null` |
|---|---|---|---|---|---|
| CsvHelper | 127 | 83 | 0 | 16 | 1 |
| FFMpegCore | 21 | 21 | 0 | 3 | 3 |
| FluentValidation | 68 | 174 | 0 | 1 | 0 |
| Jellyfin | 38 | 94 | 0 | 1,035 | 1,371 |
| Newtonsoft.Json | 638 | 948 | 4 | 5 | 1 |
| OpenTelemetry | 193 | 364 | 0 | 65 | 74 |
| Polly | 372 | 52 | 0 | 85 | 45 |
| Serilog | 45 | 59 | 0 | 14 | 1 |
| Total | 1,502 | 1,795 | 4 | 1,224 | 1,496 |

Equality operators in seven of the eight repositories (3,301 checks); patterns in Jellyfin, which alone has most of the
2,720.

### Choices

1. **`equality_operator` by default**, `pattern_matching` as the option (`stylebro_null_check_style`).
2. **`pattern_matching` by default**: the newer form, never calls a user-defined `==`; needs C# 9 for `is not null`.
3. **Off by default**, on only when the option is set (like Roslynator).

`null == x`: with `pattern_matching` it becomes `x is null` directly (there is no `null is x`); with
`equality_operator` it is left to BRO1103. Both rules report `null == x` in pattern mode, and their fixes converge in
one `dotnet format` run (BRO1133 finds the check again by its start, which the swap keeps; samples/Messy).

### Answer

**Choice 2:** `pattern_matching` (owner's decision, 2026-10-04): the form that never calls a user-defined `==`;
configurable; records skipped. The survey's majority (choice 1, the agent's proposal) stays available as
`equality_operator`. At warning in the preset; off after `stylebro-migrate`.
## Split conditional expressions (StyleCop issue #651) (2026-10-04)

### Question

BRO1524 ([beyond-stylecop.md](beyond-stylecop.md#from-stylecops-issue-tracker) #1): once a conditional expression is
split, the condition, `? a` and `: b` each start their own line. Which side of the line break do `?` and `:` go on, what
counts as split, and what about chains (`a ? x : b ? y : z`), the open question in #651 (a contributor wanted
`cond ? value :` per line allowed, the maintainer preferred the strict rule or a switch expression)?

Survey of the eight reference repositories (scripts/realworld/repos.psd1), 2,283 conditional expressions, 719 with a line
break right next to `?` or `:`:

| Shape | Count | Repositories |
|---|---|---|
| Every part on its own line, `?`/`:` first | 631 | all eight (Jellyfin 332, OpenTelemetry 189, Newtonsoft.Json 53, Polly 23, CsvHelper 22, Serilog 7, FluentValidation 4, FFMpegCore 1) |
| Every part on its own line, `?`/`:` last | 41 | Jellyfin 20, OpenTelemetry 15, Serilog 5, Polly 1 |
| A token with its line break on the wrong side only (BRO1520's) | 8 | Jellyfin 4, OpenTelemetry 4 |
| Comment next to `?`/`:` | 8 | |
| `?` or `:` sharing its line with both neighbors (this rule) | 31 | Jellyfin 19, Newtonsoft.Json 6, OpenTelemetry 4, FluentValidation 1, Serilog 1 |
| ... of which in chains | 18 | Jellyfin 11 (both list styles), Newtonsoft.Json 5 (`cond ? value :` per line), OpenTelemetry 2 |

Ten more conditionals had a part spanning lines (a lambda, a wrapped call) with no line break next to `?`/`:`; none had
a moved part spanning lines.

### Choices

1. **Each conditional on its own, chains skipped** (a conditional whose `: b` is a conditional, or that is one's `: b`):
   the 13 non-chain findings are fixed; the 18 chain links stay as their authors laid them out.
2. **Each conditional on its own, chains included:** a chain laid out as a list becomes a staircase (each link one level
   deeper), which nobody in the survey writes.
3. **Chains as one list** (each `: condition ? value` on its own line): a second layout to define and fix, for 18 cases.

Side of the line break: BRO1520's setting (`dotnet_style_operator_placement_when_wrapping`, default `beginning_of_line`,
the survey's majority 631 to 41), so the two rules never disagree. Split = a line break right next to `?` or `:` (not
inside a part). Indentation of the new line: like the first part that already starts a line. Preset: warning;
`stylebro-migrate` writes it as `none` (no StyleCop rule asks for it).

### Answer

Choice 1 (agent's proposal, 2026-10-04, for the owner to review).
## Summary layout: one line or three (2026-10-04)

### Question

[beyond-stylecop.md](beyond-stylecop.md) #5: should a `<summary>` be written on one line
(`/// <summary>Gets the name.</summary>`) or with its tags on lines of their own, which elements does the rule check,
and what's the default?

Facts: Roslynator's [RCS1253](https://josefpihrt.github.io/docs/roslynator/analyzers/RCS1253) (off by default, needs
`roslynator_doc_comment_summary_style = single_line | multi_line`) checks only `<summary>`: `multi_line` reports a
summary written on one line; `single_line` reports a multi-line one whose text is one line, unless it has `<code>`,
`<list>` or `<para>`, with no length limit. Meziantou's
[MA0211](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0211.md) (off by default) reports
non-empty one-line summaries; [MA0177](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0177.md)
(off by default) reports any top-level element (summary, param, returns, ...) on several lines whose content is one
line and whose joined line fits within `max_line_length` (no limit when it isn't set). None of them moves the tags of a
summary whose text starts or ends on a tag's line (`/// <summary>Draws the shape` + `/// on the screen.</summary>`).

Survey of the eight reference repositories (scripts/realworld/repos.psd1), summaries whose text is one line (no
`<para>`/`<code>`/`<list>`), one-line form / tags on their own lines: Jellyfin 19/8,507, Newtonsoft.Json 11/1,650,
Polly 26/1,360, OpenTelemetry 75/1,163, CsvHelper 15/725, Serilog 1/456, FluentValidation 1/380, FFMpegCore 0/88;
in all 148/14,329. No summary had text on a tag's line. (StyleBro's own code is the other way round: 291/4.)

### Choices

1. **One rule, summary only, `stylebro_summary_layout = multi_line | single_line_when_fits`**, `max_line_length` for
   "fits" (no limit when unset, like MA0177); a summary with text on a tag's line gets its tags on lines of their own
   in both layouts.
2. Every top-level element (like MA0177): `<param>`, `<returns>` and `<remarks>` are usually written on one line even
   by teams that write summaries on three, so one option for all would fight most codebases.
3. A default limit (say 120) when `max_line_length` isn't set: the rule only joins text that is already one line, so
   the joined line is the text's line plus about 20 characters; a limit nobody configured would be a guess.

Proposed default: `multi_line` (the survey: 99% in every repository), BRO1616 at warning in the preset, off after
`stylebro-migrate` (no StyleCop rule asks for it). StyleBro's repository sets `single_line_when_fits` for its own code.

### Answer

**Choice 1**, default `multi_line`. Default chosen from the survey (agent's proposal, 2026-10-04, for the owner to
review); configurable. Implemented as [BRO1616](rules/BRO1616.md).

## Placement of operators, `=>` and `=` when a line wraps (2026-10-04)

### Question

The first rules beyond StyleCop ([beyond-stylecop.md](beyond-stylecop.md) #1 and #2) put a token on one side of a line
break: binary operators and `?`/`:` (Roslynator RCS0027/RCS0028), `=>` (RCS0032), `=` (RCS0052). What shape, which
tokens, and which defaults?

Facts: the SDK has `dotnet_style_operator_placement_when_wrapping` (default `beginning_of_line`) but no rule enforces
it. Probed with SDK 10.0.401: with the key set to `end_of_line`, `dotnet_diagnostic.IDE0055.severity = warning` and
beginning-of-line operators, `dotnet format --severity info` changed none of them, and the reverse (`beginning_of_line`
with end-of-line operators) didn't either; a build with `EnforceCodeStyleInBuild` reported no IDE diagnostic. Roslynator's
four rules are off by default and have no default value (their options must be set). There's no SDK key for `=>` or `=`.

Survey of the eight reference repositories (scripts/realworld/repos.psd1), tokens with a line break on one side,
beginning of line / end of line:

| Tokens | Beginning | End | Repositories (beginning / end) |
|---|---|---|---|
| `&&`, `\|\|` | 1,785 | 370 | Jellyfin 1,497/96, OpenTelemetry 165/175, Newtonsoft.Json 94/36, CsvHelper 17/17, FluentValidation 9/3, Serilog 3/33, Polly 0/10 |
| `+` | 134 | 168 | Newtonsoft.Json 41/15, Jellyfin 37/6, OpenTelemetry 56/62, CsvHelper 0/39, Polly 0/25, Serilog 0/18, FFMpegCore 0/3 |
| `??` | 128 | 27 | Jellyfin 63/18, OpenTelemetry 51/0, Newtonsoft.Json 10/5, FFMpegCore 4/2, Serilog 0/2 |
| other binary | 13 | 45 | Newtonsoft.Json 0/29, OpenTelemetry 1/9, Jellyfin 12/5, Polly 0/2 |
| `?` / `:` | 641 / 650 | 48 / 63 | beginning in every repository; Serilog closest (8/6) |
| `=>` expression bodies | 1,259 | 901 | beginning: OpenTelemetry 676/335, Jellyfin 398/14, FluentValidation 52/3, Serilog 2/0; end: Polly 130/543, FFMpegCore 0/3, CsvHelper 0/2; Newtonsoft.Json 1/1 |
| `=>` switch arms | 8 | 14 | OpenTelemetry 4/7, Jellyfin 3/5, Polly 0/2, Newtonsoft.Json 1/0 |
| `=>` lambdas | 9 | 5,299 | end everywhere (block bodies included) |
| `=` assignments and initializers | 6 | 518 | end in all eight |

All operators together: beginning in six repositories, end in Serilog, FFMpegCore even.

### Choices

1. **One rule per token kind, one analyzer and one fix:** BRO1520 operators (follows the SDK key), BRO1521 `=>`
   (`stylebro_arrow_placement_when_wrapping`), BRO1522 `=` (`stylebro_equals_placement_when_wrapping`); each can be
   turned off on its own.
2. **One rule with three options:** fewer ids, but a team that wants only the operator rule has to set the other two to
   "don't care", which needs a third option value.
3. **Two rules** like the survey's pairing (operators; `=>` and `=` together): `=>` and `=` have opposite majorities, so
   they need separate options anyway.

Proposed defaults: operators `beginning_of_line` (the SDK's default, and the survey's majority), `=>` `beginning_of_line`
(four repositories to three, 1,259 to 901 tokens), `=` `end_of_line` (518 to 6). Scope of `=>`: expression bodies and
switch arms; lambdas left out (5,299 to 9 at the end, and a block body's `{` belongs on its own line). Preset: all three
at warning; `stylebro-migrate` writes them as `none` (no StyleCop rule asks for them).

### Answer

**Choice 1** (user's decision, 2026-10-04), with these defaults, all configurable: operators `beginning_of_line` (the
SDK's default and the survey's majority), `=` `end_of_line` (the survey), `=>` `end_of_line` (the owner's choice over the
proposed `beginning_of_line`: one convention for every `=>`, like lambdas and StyleBro's own code). All three at warning
in the preset; off after `stylebro-migrate`.

## Namespace names (SA1300 for namespaces) (2026-10-04)

### Question

BRO1309 skipped namespaces, so SA1300's check on namespace names had no StyleBro rule. A namespace rename changes the
full name of every type in it and can break things a code rename can't see (details and experiments in
[proposals/namespace-names.md](proposals/namespace-names.md)): plain embedded resources follow `RootNamespace`, not the
code; strings and stored data with type names (`Type.GetType`, `$type`); Razor and XAML references; config files. In
22 public repositories every lower-case namespace part was deliberate (brand names like `iText`, `iOS`; culture
codes). Open questions: ship a rule at all, as a separate id and on or off by default; should `stylebro-migrate` turn
it on with SA1300; report the root namespace (which the fix can't rename) or not; rename public namespaces in
libraries; and what to do about resources in folders, config files and XAML.

### Choices

1. **Ship opt-in:** a separate rule (BRO1312), off in the descriptor and the preset, with the prototype's guards;
   `stylebro-migrate` turns it on when SA1300 is on; the root namespace isn't reported; public namespaces are renamed
   like BRO1309 renames public types; the resource, config and XAML limits are documented.
2. **Ship with the root namespace reported but not fixed** (a diagnostic `dotnet format` can't fix).
3. **Don't rename public namespaces** (skip namespaces with a public type, or packable projects).
4. **Keep the skip** and record it with the findings.

### Answer

Ship opt-in (user's decision, 2026-10-04). Choice 1: BRO1312 off by default, on through `stylebro-migrate` with
SA1300; root namespace unreported; public namespaces renamed; resources in folders, config files and XAML documented as
limits (no package change to expose embedded resources).

## SA1108: where a comment between a statement's header and its block goes (2026-10-04)

### Question

SA1108 reports a comment between a statement's header and its `{` (`if (x) // c`, `else // c`, `catch (E) // c`), and
StyleCop has no fix. StyleBro needs one, so where should the comment go? Found in the reference repos: 34 (all `//` at
the end of the header line); 4 of them end a condition split over lines and explain its last line, not the block.
Details: [proposals/sa1108.md](proposals/sa1108.md).

### Choices

- **A:** into the block, on its own line right after `{`; one behavior for every statement and clause.
- **A':** like A, but headers spanning several lines aren't reported (each clause judged on its own).
- **B:** on its own line above the statement; `else`/`catch`/`finally`/`else if` have no "above" and would need A.
- **C:** at the end of the `{` line (`{ // c`); the smallest move, an unusual layout.
- **D:** report only, no fix; not allowed by StyleBro's design, so the rule would stay dropped.

### Answer

**A'** (user's decision, 2026-10-04). Implemented as [BRO1132](rules/BRO1132.md); the multi-line header skip is a
documented difference from StyleCop.


## IDE0055 in multi-targeted repositories (2026-10-03)

### Question

With IDE0055 (the SDK's formatting rule) at warning, `dotnet format` crashes on multi-targeted repositories and writes
nothing (Serilog, Newtonsoft.Json; no StyleBro involved). `init` wrote it as a suggestion there, so formatting wasn't
enforced. Investigated: the crash is Roslyn's linked-file merge (`LinkedFileMergeConflictCommentAdditionService`:
"Changes must be within bounds of SourceText"); IDE0055's fix edits each framework's copy of a file, each copy sees
other `#if` code, the results can't be merged. The whitespace pass doesn't crash but only formats the first framework's
code (Serilog: 280 warnings left, all inside `#if FEATURE_...` blocks net471 doesn't compile). Loading the projects
for one framework at a time (`TargetFramework` as an environment variable) leaves nothing to merge: once per framework,
Serilog went from 505 IDE0055 warnings to 0, no crash, no conflict markers, builds. How should StyleBro offer that?

### Choices

1. **`stylebro-migrate format`:** a command that runs `dotnet format` once per target framework; `init` writes IDE0055
   at warning everywhere and the docs use the command.
2. **Document a script** for multi-targeted repos; `init` keeps the suggestion.
3. **Build-only enforcement:** the severity in a `.globalconfig` (the build honors it, `dotnet format`'s style pass
   doesn't): no crash, but `#if` code the first framework doesn't see has to be fixed by hand.
4. **Leave as is** and record the findings.

### Answer

**Choice 1** (user's decision). Each run uses a temporary solution filter with the projects that target that
framework, so no project is loaded for a framework it doesn't have. Known limit: code that needs different
indentation per framework (an `else` inside `#if`) can't satisfy IDE0055 for all of them (Newtonsoft.Json: 32 lines).

## After StyleCop parity (2026-10-03)

### Question

StyleCop parity is essentially done: of StyleCop's 197 rules, 104 are StyleBro rules, 45 SDK settings and 43 dropped by
design, with one rule (SA1316) and two small gaps left as "maybe". What should the backlog grow into next?

### Choices

1. **Hardening:** more real-world repos in the PR pipeline (each new one has found bugs), and making IDE0055 safe in
   multi-targeted repos.
2. **Finish the maybes:** SA1316 (tuple element casing), SA1108 (comments inside block statements), namespace names
   (BRO1309). Completes parity, little real-world impact.
3. **Adoption polish:** getting-started docs, the Visual Studio/Rider experience, a StyleCop migration sample.
4. **Beyond StyleCop:** survey other analyzers for fixable style rules the SDK doesn't cover.

### Answer

**All four** (user's decision), in this order. The items are in [backlog.md](backlog.md) under "Work beyond single
rules". IDE0055 is an investigation, not a reimplementation: it's the SDK's whole formatter, and analyzers can't
reference the formatting engine.

## Follow the SDK's .editorconfig settings where they exist (2026-10-03)

### Question

StyleBro reads the SDK's layout settings (`indent_*`, `csharp_new_line_before_*`) and rule severities, but for style
choices it follows StyleCop's behavior plus its own `stylebro_*` keys, even where the SDK has a key for the same choice:
naming rules (`dotnet_naming_rule.*`), `this.` qualification, `csharp_prefer_braces`,
`dotnet_style_require_accessibility_modifiers`, `dotnet_style_parentheses_*`, `end_of_line`. A team whose
`.editorconfig` says one thing can get a StyleBro fix that does another.

### Choices

1. **Keep StyleCop's behavior and `stylebro_*` keys only.** Predictable for StyleCop migrations; `stylebro-migrate`
   writes the keys. But teams configuring the SDK way have to say everything twice.
2. **Read the SDK keys where they exist**, with StyleCop's behavior as the default when they aren't set.

### Answer

**Choice 2** (user's decision). Precedence: a `stylebro_*` key, then the SDK key, then StyleCop's behavior. The list of
keys and what each changes is in [backlog.md](backlog.md).

To watch: the preset sets some of these SDK keys itself (`dotnet_style_qualification_for_* = false`,
`csharp_prefer_braces = true`, `dotnet_style_require_accessibility_modifiers = for_non_interface_members`,
`dotnet_style_parentheses_*`), and `stylebro-migrate` writes them too. Once they're read, those values decide behavior,
so each has to be checked against what the rule does today (e.g. qualification `false` would make BRO1131 drop `base.`
instead of writing `this.`; the preset and migrate values may need to change, or the rule may only follow an explicit
setting outside the preset).

## StyleBro versions of SDK rules that break multi-targeted projects (2026-10-03)

### Question

StyleBro leaves formatting to the .NET SDK where the SDK already covers it: `stylebro-migrate init` turns on the SDK's
IDE rules instead of StyleBro shipping its own. But in multi-targeted projects (`<TargetFrameworks>` with several
frameworks), some of the SDK's own fixers break `dotnet format`, even without StyleBro installed. Measured on Serilog
and Newtonsoft.Json:

| Rule | StyleCop rules it replaces | What goes wrong |
|------|----------------------------|-----------------|
| IDE0011 (braces) | SA1503, SA1519, SA1520 | Crashes Roslyn's linked-file merge; `dotnet format` writes nothing |
| IDE0055 (formatting, at warning) | SA1000-SA1028, SA1134, SA1137, SA1500, SA1107 | Crashes the same way |
| IDE0040 (access modifiers) | SA1400, SA1205 | Writes merge conflict markers into the source |
| IDE0047 (unnecessary parentheses) | SA1119 | Conflict markers |
| IDE0048 (parentheses for clarity) | SA1407, SA1408 | A broken edit in one framework's code |
| IDE2000/IDE2002/IDE2003 (blank lines) | SA1507, SA1508, SA1513 | One blank-line conflict per repo |

Today `init` writes these 8 rules as `suggestion` in multi-targeted repos, so they're reported in the IDE but never
fixed or enforced there.

### Choices

1. **Keep the SDK rules and the workaround.** No new code, and it stays true to "formatting belongs to the SDK". But
   multi-targeted repos (common for libraries) don't get these StyleCop rules enforced at all.
2. **StyleBro versions of the affected rules.** BRO rules with fixes built on `LinkedFileFixAllProvider`, which already
   handles multi-targeted files correctly. Enforced everywhere, at the cost of duplicating SDK functionality and
   maintaining it.
3. **Report the bugs upstream and wait.** Right thing to do in any case, but no timeline.

### Answer

**Choice 2** (user's decision). StyleBro ships its own versions of these rules; `init` and `stylebro-migrate` turn the
BRO rule on and the SDK rule off. The SDK bugs should still be reported upstream.

Scope, as far as a BRO version is feasible: braces (IDE0011), access modifiers (IDE0040), parentheses (IDE0047,
IDE0048) and the blank-line rules (IDE2000, IDE2002, IDE2003). IDE0055 is the SDK's whole formatter and isn't
reimplemented; the safe part (`dotnet format`'s whitespace pass, which isn't affected) keeps doing that job, and only
the rule's warning-level enforcement stays a suggestion in multi-targeted repos.

## Which literal suffixes BRO1135 upper-cases (2026-10-04)
## Braceless jump statements on the `if` line (2026-10-04)

Agent's proposal, 2026-10-04, for the owner to review.

### Question

StyleCop issue [#1563](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1563) asks for upper-case literal
suffixes (`50l` -> `50L`, `50.4m` -> `50.4M`). In the issue, maintainers and commenters wanted `L` but preferred lower
case for `m`/`f` (and the exponent's `e`); Sonar's S818 reports only `l` and leaves `ul` alone; the compiler warns
(CS0078) about `l` only. Which suffixes should the rule cover?

### Choices

1. **Integer suffixes** (`u`, `l` and their combinations): `1l` -> `1L`, `1u` -> `1U`, `1ul` -> `1UL`. One way to write
   an integer suffix; real suffixes stay as written.
2. **`l` only** (S818, CS0078): the one letter that looks like a digit.
3. **Every suffix** (the issue as written): also `f`, `d`, `m`.

### Answer (proposed)

**Choice 1** (BRO1135). Survey of the 8 reference repositories (line-based, outside strings and comments): lower-case
integer suffixes only in OpenTelemetry (24 `u`, 1 `ul`, e.g. `0x9E3779B1u`), no `l` anywhere; real suffixes are
written in lower case almost everywhere (e.g. Newtonsoft.Json 517 lower vs 40 upper, Jellyfin 92 vs 0), so choice 3
would rewrite hundreds of literals against the common style. Choice 2 would report nothing in these repos; choice 1
finds 25. Not a StyleCop rule, so `stylebro-migrate` turns it off; the preset turns it on.
StyleCop issue [#2252](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2252) (12 reactions; #1175 asked
the same) wants `if (x) return;` without braces while every other child statement still needs them. Which statements,
where, and how does it combine with the other brace rules?

### Choices

1. **Jump statements on the `if` line only** (the request as written): `return`, `throw`, `break`, `continue`, plus
   `goto` (also a jump statement in the C# spec) and `yield break` (leaves an iterator like `return`). Not on the next
   line (a statement added below looks like part of the `if`), not after `else`.
2. **Any single-line statement on the `if` line** (`if (x) a++;`). The issue's author ruled this out explicitly.
3. **A second option for "next line too"** (sharwell's question 2 in the issue). Nobody asked for it: those who wanted
   the next line wanted it for every statement, which `csharp_prefer_braces = when_multiline` already allows.

### Answer (proposed)

**Choice 1**, as `stylebro_allow_single_line_jump_statements = true|false` (default `false`: StyleCop's behavior). It
only exempts statements from BRO1514. In an `if`/`else` chain where another clause has braces or gets them in the same
run, BRO1516 still wants them on the jump too, so the chain ends up consistent in one `dotnet format` run.
`when_multiline` already allows all single-line children (the option adds nothing there), and BRO1508/BRO1509 aren't
involved (no block, so nothing to collapse or expand). No `stylecop.json` setting maps to it.

Survey of the 8 reference repos (line-based count of braceless `if (...) <jump>;` on one line / jump on the next line /
another statement on the `if` line): CsvHelper 27 / 0 / 0, FluentValidation 22 / 13 / 2, Newtonsoft.Json 19 / 33 / 1,
Serilog 31 / 81 / 0, FFMpegCore, Jellyfin, OpenTelemetry and Polly 0 / 0 / 0. Where braceless `if`s exist, the
same-line form is almost always a jump, which is what the option allows.
