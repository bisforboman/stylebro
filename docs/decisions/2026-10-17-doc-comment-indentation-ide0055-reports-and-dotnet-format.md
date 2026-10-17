# Doc comment indentation IDE0055 reports and dotnet format leaves (2026-10-17)

## Question

Trial 27 (PalCalc): a documentation comment whose first `///` line is indented with spaces and whose other `///` lines
(and the member's line below it) with tabs at the same column is left alone by `dotnet format whitespace`, but the build's
IDE0055 (EnforceCodeStyleInBuild) reports those lines. `stylebro-migrate format --verify-no-changes` said clean while that
build failed. Probed with SDK 10.0.402: the formatter compares columns there (with `tab_width = 8` the same lines are
rewritten), IDE0055 compares the text. What should format do?

## Choices

1. **Fix it ourselves and report it upstream**: format rewrites the indentation of exactly those lines to what the
   settings ask (indent_style, tab_width), at the same column, and verify counts them as IDE0055 findings; a Roslyn issue
   describes the mismatch.
2. Report it upstream only and document it: verify keeps saying clean while the build fails.
3. Only make verify report them, and leave the fix to the user.

## Decision

**Choice 1** (owner's decision 2026-10-17: "fix it ourselves + report upstream"). Only the lines after a documentation
comment's first line (its other `///` lines and the line of the code it documents) at that line's column, written with
other characters than the settings ask, when the first line is written as they ask (otherwise the whitespace pass rewrites
the whole comment itself). Only their leading whitespace changes; files the whitespace pass leaves out (IDE0055 off, a
CSharpier folder, `--exclude`, submodules, generated code) are left out too. Code: `src/StyleBro.Migrate/DocIndentation.cs`.
