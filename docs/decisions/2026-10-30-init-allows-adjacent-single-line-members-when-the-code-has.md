# Init allows adjacent single-line members when the code has some (2026-10-30)

## Question

Trial 63, mono/t4: init left BRO1505 on, and format added about 130 blank lines between neighbouring one-line members
(expression-bodied methods such as `public void Clear () => session.Clear ();`) in 31 files. The repository groups such
members about 121 times and separates them about 48 times. BRO1505 has `stylebro_allow_adjacent_single_line_members`
(decision of 2026-10-07), but init never counted it. Should init count it?

## Choices

1. **Any 3 allow it** (recommended): like `csharp_preserve_single_line_statements`, from 3 pairs of one-line members next
   to each other, init writes `stylebro_allow_adjacent_single_line_members = true`. It only allows grouping; blank lines
   already there stay.
2. Majority decides: true only when most such pairs sit together.
3. Leave as is: StyleCop's default; init doesn't count it.

## Decision

"Any 3 allow it" (owner, 2026-10-30). The pairs the key would allow count for it and no longer for BRO1505's severity.
