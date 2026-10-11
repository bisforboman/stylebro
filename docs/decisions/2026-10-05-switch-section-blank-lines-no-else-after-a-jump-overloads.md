# Switch section blank lines, no `else` after a jump, overloads together (2026-10-05)

## Question

Three candidates from [beyond-stylecop.md](../beyond-stylecop.md) (#6, #16, #17): Roslynator's RCS0061 (blank line between
switch sections), Meziantou's MA0071 / Roslynator's RCS1211 (no `else` after a branch that ends in a jump) and Sonar's
S4136 (overloads next to each other). Defaults, and how each agrees with the rules that touch the same code: BRO1519
(SA1513) wants a blank line after a case block's `}` before the next label (kept in "StyleCop's open bugs", #3568), and
BRO1001 sorts by StyleCop's access order.

## Choices

- RCS0061: `include` / `omit` / `omit_after_block`, on or off in the preset; what to do where BRO1519 wants the opposite.
- MA0071: on or off; `else if` chains (whole chain, or skip); how much to recognize as "ends in a jump".
- S4136: a new rule or a BRO1001 option; which members count as overloads.

## Decision

- **BRO1526** (owner): option `stylebro_blank_line_between_switch_sections = include | omit | omit_after_block`, default
  `include`, on in the preset, off after `stylebro-migrate`. Agent's resolution of the conflict: after a section whose
  `}` BRO1519 judges (a multi-line block or if/else at the end), BRO1526 reports nothing when BRO1519 is on, so the
  two never undo each other; with `omit`, a blank line stays there unless BRO1519 is turned off (documented). A
  one-line block isn't BRO1519's and follows the setting. Comments and directives between sections are skipped; switch
  expression arms aren't sections.
- **BRO1143** (owner): off by default (`isEnabledByDefault: false`, preset `none`), it's a matter of taste. Agent's
  choices: `else if` chains skipped entirely (removing the first `else` would only make a new candidate); "ends in a
  jump" is syntactic (a jump statement, or a block whose last statement is one); skipped when a name declared in the
  body is used in the enclosing block, for `using` declarations, comments around the `else`, `#if`, and lines that
  can't simply be moved out (multi-line strings and comments, unusual indentation). An `if` branch without braces next
  to an `else` block is BRO1516's first (the chain is inconsistent; without the `else` it would become BRO1514's, which
  may already have run), so the `else` goes on the next run.
- **BRO1001 option** (owner): `stylebro_keep_overloads_together = false | true`, default `false` (StyleCop's order).
  Agent's choices: methods only (constructors and indexers are kinds of their own, so always together; operators
  aren't grouped by symbol, so `==`/`!=` pairs stay); a group sorts at the place of its overload that sorts first, the
  other overloads right behind it in their own order; within a region; explicit interface implementations group among
  themselves.
