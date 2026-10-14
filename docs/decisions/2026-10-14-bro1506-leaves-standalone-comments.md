# BRO1506 leaves standalone comments (2026-10-14)

## Question

Trial 21, AzureEventGridSimulator: BRO1506 (SA1512) removed the blank line below `//` comments that have a blank line
above them too: notes about the code above and section headers. In 10 files they ended up glued onto unrelated code
below (`EventGridEvent.cs`). Should BRO1506 still report a comment that stands on its own like that?

## Choices

1. **Leave standalone comments**: a run of `//` lines with a blank line right above and right below it (`#region` lines
   next to it don't count) isn't reported. Still reported: a comment right below `{`, below code without a blank line
   between, or before a `}`. A new key `stylebro_blank_line_below_standalone_comments = allow | remove` (default
   `allow`); `stylebro-migrate` writes `remove` where SA1512 is on, so StyleCop-clean repos stay clean.
2. Keep SA1512's behaviour: every comment followed by a blank line is reported.

## Decision

**Choice 1** (owner's choice 2026-10-14). Documented on the rule page and in differences-from-stylecop.md.
