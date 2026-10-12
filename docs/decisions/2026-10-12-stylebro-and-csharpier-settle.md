# StyleBro and CSharpier settle (2026-10-12)

## Question

The owner's goal (2026-10-12): in a repository that formats with CSharpier, after `stylebro-migrate init --write`,
`stylebro-migrate format` and `dotnet csharpier format .`, both `stylebro-migrate format --verify-no-changes` and
`dotnet csharpier check .` must be clean. With only the layout rules off (2026-10-11-csharpier-in-init.md), StabilityMatrix
still had 16 files CSharpier rejected after StyleBro's format (fixes that rewrite code: BRO1149 merging `if`s, BRO1407
adding parentheses, BRO1141 removing `()`), and after CSharpier StyleBro changed 3 more files: BRO1515 wanted braces on
statements CSharpier had just split over lines. How should the fixes know about CSharpier, and how far should they follow
its layout?

## Choices

1. **A key init writes, and fixes follow CSharpier's line width where it is cheap** (agent's proposal):
   `stylebro_layout_by = csharpier` and `stylebro_layout_print_width` (CSharpier's printWidth). The brace rules count a
   statement CSharpier will split (longer than the width) as multi-line; BRO1149 writes the merged condition the way
   CSharpier does. Other rewrites are left to CSharpier, which StyleBro then accepts.
2. Reproduce CSharpier's layout in every fix that rewrites code (a reimplementation of CSharpier's printer).
3. No key: run CSharpier inside `stylebro-migrate format` (a dependency on the repository's tool, and on `dotnet tool
   restore` having run).

## Decision

**Choice 1** (agent's proposal; owner's goal and suggested key). The analyzers can't read `.csharpierrc`, so `init` and
`--write` copy its `printWidth` into `stylebro_layout_print_width` (else `max_line_length`, which CSharpier reads when
there is no `.csharpierrc`, else 100). With `stylebro_layout_by = csharpier`:

- BRO1514-BRO1516: a child statement alone on its line (where CSharpier puts it) longer than the width, or, for
  `csharp_prefer_braces = when_multiline`, a header up to the child longer than it, is multi-line. Overestimating only
  adds braces CSharpier keeps; underestimating is what made the second round.
- BRO1149: the merged condition on the `if` line when it fits, else every `&&` operand on its own line one level deeper,
  the operator first, `)` on its own line (CSharpier's layout). An operand over several lines or longer than the width
  keeps the usual layout: CSharpier would break it further, which this doesn't model.

Measured on StabilityMatrix (CSharpier 1.0.1, printWidth 110) with the real-world script's CSharpier mode: files CSharpier
rejects after StyleBro's format 16 -> 6 (left: BRO1141 making an initializer fit on one line, BRO1407 parentheses, and
BRO1149 merges with a multi-line operand), files StyleBro changes after CSharpier 3 -> 0. `stylebro-migrate --write`
detects CSharpier the same way and reports the StyleCop rules only the turned-off rules replaced as "CSharpier decides it".
