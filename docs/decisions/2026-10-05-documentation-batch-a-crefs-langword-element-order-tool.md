# Documentation batch A: crefs, langword, element order, tool markers (2026-10-05)

## Question

Four candidates from [beyond-stylecop.md](../beyond-stylecop.md) (StyleCop issues #758 and #3546/#1490, Meziantou
MA0154, survey #15): should they be built, on by default, and what does `stylebro-migrate` do with them?

## Choices

1. All on in the preset, off after `stylebro-migrate` (like the other rules StyleCop doesn't have).
2. Off by default (opt-in, like BRO1310/BRO1312).
3. Leave them in the backlog.

## Decision

The owner: **choice 1**, all eight, as BRO1136-BRO1142 and BRO1408. Object creation parentheses get
`stylebro_object_creation_parentheses = omit | include` with `omit` as the default; target-typed `new() { }` keeps its
parentheses. One rule for the three string forms (BRO1138). BRO1408 covers `enum E : int` and `class C : object` only.
The owner: build all four, on in the preset and off after `stylebro-migrate`. BRO1617 (`cref="List{T}"`), BRO1618
(`<see langword="null"/>` for a keyword alone in `<c>`; reserved keywords plus MA0154's contextual ones, kept
conservative), BRO1619 (top-level elements in the order summary, typeparam, param, returns, value, exception, remarks,
example, seealso; Roslynator's RCS1232 turned out to cover `<param>` order only, so the order was picked from Visual
Studio's `///` stub and Microsoft Learn's API pages, see the rule page). The BRO1504 option is
`stylebro_comment_blank_line_exempt_prefixes`, default empty, so BRO1504 behaves like SA1515 until a team sets it.
