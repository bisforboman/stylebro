# BRO1405 leaves a query's closing parenthesis line where it is (2026-10-19)

## Question

Trial 34, Sprache: BRO1405 removed the outer parentheses of
`((from sign in ... select Expression.Negate(factor)` + line break + `).XOr(Factor)).Token()`. The query's clause lines
moved one column left with the `(` (trial 33's alignment), but the line holding the inner `)` stayed, so it no longer
lines up under its `(`. It settles in one run and nothing reports it. Should the fix move such a line too?

## Choices

1. **Leave it** (owner's pick): cosmetic only, the formatter doesn't care, and moving lines no rule asks for adds risk.
2. Move it too: a line inside the removed parentheses that starts with the closing `)` of the query's own parentheses
   moves left with them.

## Decision

Leave it.
