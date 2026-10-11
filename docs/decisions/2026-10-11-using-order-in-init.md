# Using order in `init` (2026-10-11)

## Question

The preset always sorted usings, System first. A trial re-sorted SmartEnum's 44 files and Scrutor's 9, code whose usings
are mostly sorted but not always with System first. Should `init` keep a repository's using order?

## Choices

1. **Detect, like other conventions**: count files with 2 or more usings as sorted with System first, sorted without
   System first, or unsorted, and sort only when the code does.
2. Keep sorting everywhere (StyleCop's SA1208/SA1210 defaults).
3. Never sort in `init`; leave it to the team.

## Answer

**Choice 1** (owner: "Detect, like other conventions"). The usual thresholds over files: mostly sorted writes
`dotnet_sort_system_directives_first = true` (or `false` when the files that tell the two apart clearly put System
elsewhere); mostly unsorted or mixed (also sorted but with System first in some files and not in others, SmartEnum: 50
and 28) writes nothing and a comment saying how to turn sorting on; too few files writes StyleBro's default (`true`).
Probed with `dotnet format` 10.0.401: it sorts usings whenever `dotnet_sort_system_directives_first` or
`dotnet_separate_import_directive_groups` is set, to any value, in the style pass (`dotnet format whitespace` doesn't);
`dotnet_diagnostic.IDE0055.severity = none` doesn't stop it, and an `.editorconfig` can't unset a key the preset's global
config sets. So the preset no longer sets either key, and `init` writes the sort key into `.editorconfig` only where the
code is sorted. Package users who don't run `init` get no sorting from `dotnet format` (the IDE's own default is unchanged).
`dotnet_separate_import_directive_groups` isn't detected: with `false` (Roslyn's default) sorting keeps blank lines
between groups that are already there.
