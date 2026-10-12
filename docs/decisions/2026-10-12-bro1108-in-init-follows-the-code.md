# BRO1108 in init follows the code (2026-10-12)

## Question

metadata-extractor groups the arguments of split calls (`GetIndexedDescription(TagX, 1,` and `"a", "b")` on the next
line). `init` left BRO1108 on, and `format` put every item on its own line in 25 files. Should `init` turn BRO1108 off
for code that groups items?

## Choices

1. **Detect, like other conventions**: count split argument and parameter lists as one item per line or grouped.
2. Keep BRO1108 on (StyleCop's SA1117 default).
3. Turn BRO1108 off in `init` everywhere.

## Decision

**Choice 1** (owner: "Detect, like other conventions"). Argument and parameter lists with 2 or more items whose items
start on more than one line count as one per line (no item starts on the line where the previous one ends, BRO1108's
layout) or grouped. The usual thresholds: mostly one per line keeps BRO1108 on, mostly grouped or mixed turns it off with
a note (metadata-extractor: 104 one per line, 197 grouped). Lists whose items all start on one line say nothing. A
StyleCop migration keeps SA1117's setting. With BRO1108 off, BRO1107 only moves the first item (since #123), so grouped
items stay grouped.
