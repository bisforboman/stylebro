# init counts blank lines below comments for BRO1506 (2026-10-14)

## Question

BRO1506 (no blank line below a `//` comment, StyleCop's SA1512) removed the blank line below a commented-out test method
in RecyclableMemoryStream's `UnitTests/Tests.cs`. Should `init` check what the code does first?

## Choices

1. **A convention check**: count the `//` comments BRO1506 checks (the last line of a comment group; not the file header,
   trailing comments or comments below code) with a blank line below and without. Same thresholds as the other checks
   (3 places, all agree below 10, 75 % from 10); a repository that keeps the blank line, or mixes both, gets BRO1506 off
   with init's usual explanation line. StyleCop's behavior stays the default.
2. Leave BRO1506 on everywhere (StyleCop's default).

## Decision

**Choice 1** (owner's choice 2026-10-14).
