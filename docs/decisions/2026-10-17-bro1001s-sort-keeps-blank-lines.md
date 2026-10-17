# BRO1001's sort keeps blank lines (2026-10-17)

## Question

With `stylebro_member_order_scope = fields_first` (the Microsoft style, which turns BRO1505 off), BRO1001 moved fields
from the bottom of a type to the top, and the blank-line layout stayed with the positions: the fields got the blank lines
of the members whose places they took (`private int _size;` + blank line + `private int _extra;`), and the last member
took the last field's place without a blank line (`}` glued to `public void Shrink() => _size--;`). With BRO1505 on, its
fix restored the blank lines; with it off, nothing did (trial 27). What layout should moved members get?

## Choices

1. **The sort keeps blank lines**: moved members get a normal layout (a blank line between members, as BRO1505 would want,
   also when BRO1505 is off), and members that move together keep the gap they had.
2. Keep the layout with the positions (today) and leave the blank lines to BRO1505.

## Decision

**Choice 1** (owner's choice 2026-10-17: "the sort keeps blank lines"). Two members that were neighbours and stay
neighbours keep the gap between them (fields moved up as a block stay compact). Any other slot keeps its layout, and gets
a blank line where the new neighbours need one and the old ones didn't, whether BRO1505 is on or off (a blank line BRO1519
gives the old pair counts as there). The analyzer only reports order, so it is unchanged. Also under `fields_first`, a `//`
comment above a non-field member (an unparsed `///` summary) no longer counts as a group comment that keeps the type
unsorted: the other members keep their order there, only fields move past it.
