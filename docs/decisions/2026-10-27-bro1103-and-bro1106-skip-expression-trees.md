# BRO1103 and BRO1106 skip expression trees (2026-10-27)

## Question

Trial 58 (zspitz/ExpressionTreeToString): BRO1103 (constant on the left: `'c' == s[0]` -> `s[0] == 'c'`, `Tuesday <= dow` ->
`dow >= Tuesday`) and BRO1106 (`string.Empty` <-> `""`) rewrote code inside lambdas converted to `Expression<T>`. That changes
the tree the program sees at run time (operands swapped, `<=` became `>=`, a field access became a constant), and 14 of the
repository's tests that passed before failed. What should these two rules do inside expression trees?

## Choices

- Skip them: no diagnostic inside a lambda or query converted to System.Linq.Expressions, as BRO1133, BRO1134 and the
  `Where` rule already do. A documented difference from StyleCop's SA1131/SA1122, which report there.
- Leave as is and document: StyleCop parity; the rule pages warn that code inspecting expression trees can see another tree.
- Skip only BRO1103: the swap changes the node type and operand order; `""` vs `string.Empty` stays as it is.

## Decision

"Skip them (Recommended)". `ConstantComparisons.ShouldSwap` and `EmptyStrings.ShouldReplace`/`ShouldReplaceWithLiteral` ask
`NullChecks.IsInExpressionTree` (after their syntax checks); the rule pages list it under "Not reported", and
docs/differences-from-stylecop.md says so for SA1131 and SA1122.
