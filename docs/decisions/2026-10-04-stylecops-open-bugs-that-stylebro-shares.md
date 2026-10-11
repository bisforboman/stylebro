# StyleCop's open bugs that StyleBro shares (2026-10-04)

## Question

The sweep of StyleCop's open bug reports (results in
[differences-from-stylecop.md](../differences-from-stylecop.md#stylecops-open-bugs)) found 8 that StyleBro shares. Five are
plain bugs and were fixed. Three are StyleCop's behavior that its users call a bug: should StyleBro keep matching
StyleCop there?

- [#3392](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3392) (BRO1504): a comment right after `=>` (a
  switch expression arm, also lambdas and expression bodies, #3550) gets a blank line between the arrow and the comment.
- [#2832](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2832) (BRO1501): a blank line grouping the
  `{ k, v }` entries of a dictionary initializer is removed.
- [#3568](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3568) (BRO1519): a blank line is wanted between a
  case block's `}` and the next `case` label.

## Choices

For each: change StyleBro (and say so under "Compared with StyleCop"), or keep StyleCop's behavior.

## Decision

The owner: #3392 changed, `=>` counts like `{` for BRO1504 (this reverses the earlier rejection of #3550 in
[beyond-stylecop.md](../beyond-stylecop.md)). #2832 changed too, after seeing code examples: BRO1501 leaves the brace of
an initializer's entry alone, so blank lines grouping the entries stay (first answer was "keep"). #3568: keep
StyleCop's behavior (BRO1519 wants the blank line between a case block's `}` and the next label).
