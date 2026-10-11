# What `init` does when the code has no clear convention (2026-10-10)

## Question

A trial of 0.4.0-alpha.1 on Bogus: `init` found no clear majority for several settings and kept StyleBro's defaults,
which then rewrote the code one way. Operators where a line wraps: 24 at the start, 50 at the end, and BRO1520 moved 52;
null checks: 22 `is null`, 50 `== null`, and BRO1133 made 35 edits; split lists: 14 first items on the next line, 14
after `(`, and BRO1107 moved them. In RealWorld, the only 2 private fields were `_logger` and `_mediator`: too few to
judge, so BRO1303 renamed both. What should `init` do without a clear majority?

## Choices

1. Keep StyleBro's default (as before): consistent code, at the price of churn the team didn't ask for.
2. Turn that rule off: the code stays as it is; the team can pick a form later.
3. Write whichever form has more places, even 51 %.

## Decision

The owner: "Turn that rule off" (choice 2). Details: a form is written when it has at least 75 % of at least 10 places
(as before) or, with fewer than 10 places, when all of them agree (RealWorld's 2 `_` fields -> `_camelCase`). Without
that (below 75 %, or fewer than 10 places that disagree), `init` writes `dotnet_diagnostic.<rule>.severity = none` for
the rule that enforces the setting and prints why; braces on one-line bodies get `csharp_prefer_braces =
when_multiline`, which leaves one-line bodies alone. No places at all: the default stays (nothing to churn). Settings
only the SDK formatter enforces (brace placement, `else` placement), `<inheritdoc/>` spacing (only new tags) and the
`default` literal have no StyleBro rule to turn off and keep the default. A severity the repository or its Sonar setup
sets itself stays. Also counted now: BRO1406's parentheses inside arithmetic (`a + (b * c)` vs `a + b * c`; Bogus: 50
places got parentheses), written as `dotnet_style_parentheses_in_arithmetic_binary_operators`. Superseded in part by
the next decision: a minimum of 3 places.
