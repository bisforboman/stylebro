# SA1108: where a comment between a statement's header and its block goes (2026-10-04)

## Question

SA1108 reports a comment between a statement's header and its `{` (`if (x) // c`, `else // c`, `catch (E) // c`), and
StyleCop has no fix. StyleBro needs one, so where should the comment go? Found in the reference repos: 34 (all `//` at
the end of the header line); 4 of them end a condition split over lines and explain its last line, not the block.
Details: [proposals/sa1108.md](../proposals/sa1108.md).

## Choices

- **A:** into the block, on its own line right after `{`; one behavior for every statement and clause.
- **A':** like A, but headers spanning several lines aren't reported (each clause judged on its own).
- **B:** on its own line above the statement; `else`/`catch`/`finally`/`else if` have no "above" and would need A.
- **C:** at the end of the `{` line (`{ // c`); the smallest move, an unusual layout.
- **D:** report only, no fix; not allowed by StyleBro's design, so the rule would stay dropped.

## Answer

**A'** (user's decision, 2026-10-04). Implemented as [BRO1132](../rules/BRO1132.md); the multi-line header skip is a
documented difference from StyleCop.
