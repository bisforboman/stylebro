# A global config's bulk severity sets StyleBro's rules by id (2026-10-22)

## Question

Trial 43, NServiceBus.Gateway: `src/.globalconfig` says `dotnet_analyzer_diagnostic.severity = none` (and `src/.editorconfig`
says `= default` for `*.cs`). The build still reports StyleBro's rules: the preset sets them by id, which wins over a bulk
severity. `dotnet format` treats them as hidden, so `bro format` fixed none of them, `bro check` said "Clean" (exit 0) and
`bro check --build` found 186 findings. init already writes the rules by id when an .editorconfig's bulk severity hides
them, but skipped .globalconfig files on the assumption that `dotnet format` reads no severity from a global config. That is
wrong for its analyzer pass (probed: a .globalconfig's bulk `none` or `suggestion` hides the preset's rules from it; an
.editorconfig value other than `default` overrides it there). How should StyleBro handle a .globalconfig's bulk or category
severity below warning?

## Choices

1. **Set by id, like .editorconfig** (owner's pick): init writes the hidden StyleBro rules by id in its block, so format and
   the build agree; format and check say "Not clean" with the fix when an existing setup has the gap.
2. Treat it as opting out: StyleBro's rules `none` by id for the projects under it.
3. Only report it: init and format/check name the gap, nothing is written.

## Decision

Set by id. Every `.globalconfig` in the repository counts (the SDK gives a project each one in the folders above its files),
its bulk and category severities under the .editorconfig files' own (an .editorconfig `default` doesn't override it, as in `dotnet format`).
