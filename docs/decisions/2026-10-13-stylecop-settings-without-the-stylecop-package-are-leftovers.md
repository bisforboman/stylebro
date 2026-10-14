# StyleCop settings without the StyleCop package are leftovers (2026-10-13)

## Question

Spectre.Console dropped StyleCop.Analyzers but kept lower-case `dotnet_diagnostic.saXXXX.severity = none` lines in
`src/.editorconfig`. `init` refused ("this repository has a StyleCop setup"), and the migration said "StyleCop rules on:
174", treating every rule those lines don't mention as on, although no project references StyleCop.Analyzers. `--write`
plus `format` changed 127 files, BRO1008 moving usings inside namespaces in 9 files while all 44 keep them outside. What
should `init` and the migration do with StyleCop settings no project uses?

## Choices

1. **Leftovers: `init` proceeds, the migration points to `init`** (agent's proposal). When no project references
   StyleCop.Analyzers (evaluated with MSBuild, so a reference only MSBuild sees counts) and no earlier `--write` recorded
   a version (after the package swap it's gone on purpose), its settings describe nothing the build enforces. `init`
   notes the leftover files and follows the code's conventions, ignoring the SA severities; `--write` (and the dry run)
   says StyleCop isn't installed, writes nothing and suggests `init`.
2. Migrate anyway, as before: StyleCop's defaults for every rule the settings don't mention.
3. Migrate only the rules the settings turn on explicitly.

## Decision

**Choice 1** (agent's proposal; the owner can revisit). Choice 2 is what broke Spectre.Console; choice 3 still reads
intent into lines nobody has enforced since the package went. Exception: a repository with a Sonar setup still runs the
migration (it carries the Sonar part), with the leftover note. A repository without any .csproj (nothing to evaluate)
keeps the old behaviour.

Update (2026-10-14): the Sonar exception is dropped, see [Read StyleSharp like StyleCop](2026-10-14-read-stylesharp-like-stylecop.md): with a Sonar setup the migration points to `init` too.
