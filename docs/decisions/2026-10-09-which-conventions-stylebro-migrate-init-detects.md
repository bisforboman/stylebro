# Which conventions `stylebro-migrate init` detects (2026-10-09)

## Question

A trial of 0.3.0-alpha.1 on Ocelot, eShop, AutoMapper and vs-threading: `init` only looked at private field names, and
skipped even that whenever any `dotnet_naming_rule.*` key existed (Ocelot's and eShop's are for interfaces and
constants), so Ocelot's 1,058 `_x` fields were renamed in 293 files. StyleBro's other defaults overrode conventions just
as clear: Ocelot writes `=>` at the start of a wrapped line, and [BRO1521](../rules/BRO1521.md)'s default (end of line)
moved it in 115 files.

```csharp
// Ocelot before init
private static string FormatFormCollection(IFormCollection reqForm)
    => new StringBuilder()

// After init until now (BRO1521, stylebro_arrow_placement_when_wrapping = end_of_line)
private static string FormatFormCollection(IFormCollection reqForm) =>
    new StringBuilder()

// After: init wrote stylebro_arrow_placement_when_wrapping = beginning_of_line, nothing moved
private static string FormatFormCollection(IFormCollection reqForm)
    => new StringBuilder()
```

## Choices

1. Skip by default, with a setting to include them (`stylebro_rename_public_api = true`).
2. Rename them like everything else (StyleCop reports them; its fix renames them too).
3. Report them, but never fix them.

## Decision

Skip by default, setting to include (owner, 2026-10-09). Not reported unless `stylebro_rename_public_api = true`: types,
members, parameters, type parameters, tuple elements in signatures and namespaces whose effective accessibility is
public, protected or protected internal. Internal counts as not visible, also with `InternalsVisibleTo`. Independently,
a name that a `nameof(...)` produces is never renamed (it would change the string): in the member for locals and
parameters, in the type for fields, anywhere in the solution for the rest (the fix keeps the warning then).
`stylebro-migrate` keeps the default.
1. Detect the main ones: count each setting's two forms in the code and write the majority when it is clear.
2. Only fix the field detection; document the rest.
3. Ask for each setting interactively.

## Decision

The owner: "Detect the main ones" (choice 1). Details chosen while building it: a form is written when it has at least
75 % of at least 10 places (the field threshold of the earlier decision, now with a minimum: a convention seen fewer
than 10 times says little) and isn't StyleBro's default; every count is printed, also when nothing is written. Counted
with Roslyn's syntax trees over the repository's own C# (generated code, EF Core migrations, vendored folders and
submodules don't count): private field naming, brace placement and else/catch/finally, braces on one-line bodies,
operator/`=>`/`=` placement when wrapping, trailing commas, `""` or `string.Empty`, null checks, one-line summaries,
`<inheritdoc/>` spacing, `default` literal, the closing parenthesis and first item of split lists, constructor
initializer and `where` placement, `new T()` parentheses with an initializer, blank lines between switch sections.
Keys the root `.editorconfig` sets win, then a Sonar setup's; the field style is skipped only when a naming rule
covers private instance fields (the rule BRO1303 itself follows). The same trial also made `init` and `--write` mark EF
Core migration folders `generated_code = true` (eShop: 7 migration files reformatted), and the migration write
`csharp_preserve_single_line_statements = true` (vs-threading: `delegate { /* ... */ }` split), listing SA1107 as not
expressible.

### Extended: spacing and member separation (2026-10-13, trial 14)

Bogus writes `foreach( var x in y )` in 341 places and `foreach (var x in y)` in 187; IDE0055 rewrote the 341. Kavita's
code is compact, and BRO1505/BRO1513 added blank lines in 392 and 312 files. Owner's policy, the usual thresholds:
- The spacing options IDE0055 enforces are counted (`csharp_space_after_keywords_in_control_flow_statements`, the
  `control_flow_statements`/`expressions`/`type_casts` flags of `csharp_space_between_parentheses`,
  `csharp_space_after_cast`, and the spaces inside call and parameter lists). A clear form is written; mixed code turns
  IDE0055 off with one `# init: spacing:` note (it can't be narrowed to one option), so `format` skips the whitespace
  pass. Bogus (63 %) gets IDE0055 off; Kavita too (casts: 207 `(int)x`, 220 `(int) x`). With CSharpier, column
  alignment or a team's own IDE0055 severity nothing is written; a second `init` keeps the lines.
- BRO1505's places (neighbouring type members that need a blank line) and BRO1513's (doc comments right below code)
  are counted with and without the blank line; compact or mixed code turns the rule off (Kavita: 6,033 with, 2,082
  without; 2,042 with, 1,237 without: both off).
