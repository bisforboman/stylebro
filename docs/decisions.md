# Decisions

Design questions that came up while building StyleBro, the choices considered, and what was decided. Newest first.

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

### Answer

Choice 1 (agent's proposal, 2026-10-04, for the owner to review).

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

## Braceless jump statements on the `if` line (2026-10-04)

Agent's proposal, 2026-10-04, for the owner to review.

### Question

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
