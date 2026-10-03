# Decisions

Design questions that came up while building StyleBro, the choices considered, and what was decided. Newest first.

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
