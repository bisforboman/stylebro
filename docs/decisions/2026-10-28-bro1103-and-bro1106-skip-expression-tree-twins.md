# BRO1103 and BRO1106 skip expression tree twins (2026-10-28)

## Question

Trial 59 (DelegateDecompiler): BRO1103 skips expression trees since trial 58, but the repository's tests pair each expected
tree with a compiled twin (`Expression<Func<TestEnum, bool>> expected = x => TestEnum.Bar == x;` and
`Func<TestEnum, bool> compiled = x => TestEnum.Bar == x;`), decompile the delegate's IL and compare the two. Format swapped
only the delegate's comparison (`x == TestEnum.Bar`), so two tests failed. What should BRO1103 and BRO1106 do?

## Choices

- Skip expression twins: a delegate lambda written the same way (trivia aside) as an expression-tree lambda in the same
  member is skipped like it. Narrow, syntax-only; covers the test idiom anywhere.
- Skip DelegateDecompiler users: in projects that reference DelegateDecompiler, skip every lambda and `[Computed]` body.
- Leave it: the fix is correct C#; note it in the trial, the user suppresses or baselines.

## Decision

"Skip expression twins (Recommended)". `NullChecks.IsInExpressionTreeOrTwin`: BRO1103 and BRO1106 (both styles) leave a
comparison or empty string alone in a lambda that has a syntactically equivalent lambda converted to an `Expression<...>`
in the same member.
