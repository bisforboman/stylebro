# Migrate maps StyleCop's using-group blank lines from the code (2026-10-21)

## Question

Trial 39, Open-XML-SDK (StyleCop 1.2, TreatWarningsAsErrors, StyleCop-clean): stylecop.json has
`blankLinesBetweenUsingGroups: require` with `systemUsingDirectivesFirst: false`. migrate mapped that to
`dotnet_separate_import_directive_groups = true`, and format's whitespace pass then added a blank line between usings of
different root namespaces in 342 files. StyleCop's groups aren't the SDK's: StyleCop separates System (only when it goes
first), regular, alias and static usings; the SDK splits every root namespace (DocumentFormat / Microsoft / System). Of 626
files with a root-namespace change in their usings, 557 never separate them, 4 always do, 65 mix. With `false` the SDK
neither adds nor removes those blank lines (probed). What should migrate write?

## Choices

1. **Follow the code** (owner's pick): `true` only when the code already separates root-namespace groups (init's rule: 3+
   files, 75 %), else `false`, with a note.
2. `false` when systemUsingDirectivesFirst is false; keep `true` when System goes first.
3. Keep `true`; the files get the blank lines on the first format.

## Decision

Follow the code. `Conventions.Count` counts files whose namespace usings change root namespace with a blank line at every
change or at none (`UsingGroupsKey`, mixed files not counted); `Migration.AddUsingGroups` turns StyleCop's `true` into
`false` unless `SeparatesUsingGroups` and says so in the report. A repository's own setting of the key is kept.
