# Lines whose if/else body #if decides keep their indentation (2026-10-16)

## Question

Trial 26 (NLog, also dotnet/xharness): in code like `if (c) a(); else` + `#endif` + `b();`, or `#if DEBUG` + `if (x)` +
`#endif` + `Log();`, `#if` decides whether a line is the body of an `if`/`else` (or a loop). The formatter indents it as
a body where the symbol is defined and as a statement of its own where it isn't, so `format`'s per-framework and
per-configuration runs undid each other's indentation on every run: NLog never converged ("still changing after 3
runs", exit 2). What should `format` do with such lines?

## Choices

1. Leave such lines alone: no whitespace edit on lines whose parent statement changes with `#if`; they keep their
   current indentation, other lines are still formatted.
2. Pick one build's indentation (the default configuration's) and make the others accept it.
3. Rewrite the code (braces, or move the directive) so the nesting no longer depends on `#if`.

## Decision

**Choice 1: leave such lines alone** (owner's choice, 2026-10-16). `format` parses each file that has such a shape once
per set of `#if` symbols its runs define (`IfBodies.Lines`: the first token of a line is a body of a different number of
statements in two parses where it is code) and gives those lines their indentation and line break back after every run.
A whitespace finding on them (or only a line break on the lines just above) doesn't count in the checks or in
`--verify-no-changes`. `format` names them once ("Indentation kept: ..."). NLog converges in 2 runs (one changing, one
clean).
