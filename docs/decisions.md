# Decisions

Design questions that came up while building StyleBro, the choices considered, and what was decided. Newest first.

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
