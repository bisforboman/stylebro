# Trial 58: not clean, the host soak is clean (2026-10-28)

## Question

Trial 58 (LightInject, CommandLineUtils, NPoco random, seed 58; ExpressionTreeToString targeted at #310's Visual Basic
fixes) found these, all fixed in its PR (#311):

- LightInject: StyleCop ran only in the library, so format left the tests alone; an xUnit2013 there, in the untouched code
  too, made `check --build` say "Not clean: fix them by hand" on both engines.
- ExpressionTreeToString: BRO1103 and BRO1106 rewrote lambdas converted to `Expression<T>`, which its tests render: 14 tests
  failed (owner's decision: both rules skip expression trees).
- ExpressionTreeToString: CS1685 (a type the compiler needs in two references) made `check --build` say "Not verified:
  compiled nothing", though the analyzers ran there.

CommandLineUtils and NPoco were clean and honest; #310's Visual Basic lens held. Engine soak: no fallback; trees, exit codes,
verdicts, --json, builds and tests identical on LightInject, CommandLineUtils and NPoco in both rounds; ExpressionTreeToString
differed only by the documented Visual Basic gap. Is the trial clean, and is the soak clean?

## Choices

- Trial: **Not clean** (tally stays 0) / Clean (tally 1).
- Soak: **Clean soak** / Not clean.

## Decision

Trial: "Not clean" (owner, 2026-10-28): the tally stays 0. Soak: "Clean soak": the third clean soak with the host as the
default engine.
