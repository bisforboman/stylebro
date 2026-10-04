# Decisions

Design questions that came up while building StyleBro, the choices considered, and what was decided. Newest first.

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
