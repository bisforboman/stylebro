# CSharpier in init (2026-10-11)

## Question

StabilityMatrix formats with CSharpier (`.csharpierrc.yaml`, the tool in `.config/dotnet-tools.json`, a Husky pre-commit
hook). After StyleBro's `format` run, CSharpier's check rejected 161 files it had accepted: the two formatters fight over
layout. What should `init` do in a repository that uses CSharpier?

## Choices

1. **Detect it, turn layout rules off**: IDE0055 and every StyleBro rule for something CSharpier decides go off; naming,
   ordering, documentation and readability rules stay.
2. Leave it to the team (document how to turn the rules off).
3. Refuse to run, like for a StyleCop setup.

## Decision

**Choice 1** (owner: "Detect it, turn layout rules off"). Agent's choices: CSharpier is detected by a `.csharpierrc`
(`.json`/`.yaml`/`.yml`) at the root, a `csharpier` tool in `[.config/]dotnet-tools.json`, or a `CSharpier.MsBuild`
reference in any MSBuild file. Off (`Conventions.CSharpierRules`): IDE0055 (so `stylebro-migrate format` skips the
whitespace pass; the line replaces the template's `warning`), the list layout rules BRO1107-BRO1110 and BRO1116-BRO1119,
BRO1105, BRO1111, BRO1121, the query layout BRO1127-BRO1130, the wrapping rules BRO1520-BRO1524, single-line blocks and
accessor layout BRO1508-BRO1510 and BRO1527, and the blank lines CSharpier normalizes itself (BRO1501-BRO1505, BRO1512,
BRO1517, BRO1518; BRO1504 and BRO1505 because CSharpier removes the blank line they add before a comment inside an
initializer or above `else`, and between accessors). Checked on a copy of StabilityMatrix with its own CSharpier 1.0.1:
without BRO1504/BRO1505 in the list, 47 files CSharpier had accepted were rejected, most for those blank lines. Kept: the
blank lines CSharpier keeps as written (BRO1506, BRO1511, BRO1513, BRO1519, BRO1525, BRO1526), braces on one-line bodies (BRO1514-BRO1516: not whitespace),
structural rewrites CSharpier then lays out (BRO1102, BRO1114, BRO1139, BRO1142, ...), comments and documentation. The
layout conventions are reported as `set` (`CSharpier formats it`) instead of being counted into lines of their own.
BRO1527's loop with IDE0055 on CSharpier's multi-line generic layout can't happen here: both are off.
