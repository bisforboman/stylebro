# Folders that turn every analyzer off stay off (2026-10-14)

## Question

coverlet has 12 folders whose `.editorconfig` says `root = true` and `dotnet_analyzer_diagnostic.severity = none`, with
the comment "ensure no rules are enabled for these asset source files" (test samples whose layout the tests depend on).
Trial 19 (#188) made `init` write StyleBro's rules by id at the preset's severity wherever a folder's bulk severity hid
them from `dotnet format`. In coverlet that put ~119 rules at warning into those folders; `format` then rewrote the
samples (with `root = true` and no `indent_size` there, 2 spaces became 4) and 49 tests failed. What should `init` do
in a folder that turns every analyzer off?

## Choices

- Honor the opt-out: write StyleBro's rules, IDE0055 and the other built-in rules `init` manages as `none` by id there,
  so the build (where the preset's per-rule entries win over a bulk `none`) and `format` both leave the folder alone.
- Keep #188's behavior (rules on by id) and only warn.
- Leave such folders alone entirely (the build would still report StyleBro's rules there).

## Decision

Owner's choice 2026-10-14: honor the opt-out folders. This reverses #188 for this case; a bulk or category severity of
`suggestion` (or a category's `silent`/`none` alone) still gets the rules by id at the preset's severity, as decided on
2026-10-13.

- A folder opts out when its own `.editorconfig` files set `dotnet_analyzer_diagnostic.severity` to `none` or `silent`,
  with or without `root = true`. `silent` gets the same treatment: it shows nothing in the build or in `format` either,
  and the lines are written as `none` so `format`'s whitespace pass leaves the folder out (it only skips files whose
  IDE0055 is `none`).
- The lines: every rule the preset turns on, every built-in rule `init`'s block sets, and every rule StyleBro's blocks
  further up turn on (a root block's convention or modernize lines reach a folder without `root = true` and would win
  over its bulk `none`). The team's own rules from the root `.editorconfig` aren't touched.
- A rule the folder's own `.editorconfig` sets by id stays the team's.
- A `root = true` folder that opts out gets only these lines in its block: no conventions are counted there, no
  modernize block. A folder below one that opts out needs nothing of its own.
- `stylebro-migrate format` doesn't say clean until those lines are written (the build reports StyleBro's rules there).
