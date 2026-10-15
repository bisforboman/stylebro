# A root .editorconfig that turns the Style category or every analyzer off (2026-10-15)

## Question

Trial 23 (spots b5a): the root `.editorconfig` says `dotnet_analyzer_diagnostic.category-Style.severity = none` (or
`dotnet_analyzer_diagnostic.severity = none`). `init`'s block wrote IDE0055, IDE0036 and IDE0049 = warning by id, which
win over a category or bulk severity, and `format` re-indented the code. The folder case was fixed in trial 22
(`NestedStyleOff`), the root wasn't. What should `init` do at the root?

## Choices

1. Leave the built-in rules the block would turn on by id out of the root block, so the team's category or bulk severity
   decides them; StyleBro's own rules stay on (by id where the bulk severity would hide them from `format`, as decided on
   2026-10-13 for sharpcompress).
2. Treat a root bulk `none` as opting out of everything, like a folder (2026-10-14): StyleBro's rules `none` by id too.

## Decision

**Choice 1: Keep StyleBro on** (owner's choice 2026-10-15 (confirmed); first proposed by the agent in the trial 23 task,
which asked for the folder handling at the root). StyleBro's own rules stay on by id, `init` stops turning the IDE
formatting rules back on, and says so in its output. A root bulk `none` or
`silent` next to an installed StyleBro is the sharpcompress case of 2026-10-13: the team installed StyleBro and turns the
analyzers it wants on by id, so turning StyleBro off would make `init` do nothing. The built-in rules are the SDK's, which
the bulk setting covers: their lines leave the block (IDE0055 too, so `format` skips the whitespace pass). A folder that
opts out (2026-10-14) keeps its own handling. To have StyleBro off in the whole repository, use `<StyleBroPreset>none</StyleBroPreset>`.
