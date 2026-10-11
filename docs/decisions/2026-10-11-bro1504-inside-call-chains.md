# BRO1504 inside call chains (2026-10-11)

## Question

A trial on davidfowl/TodoApi: BRO1504 (SA1515) inserted a blank line above a `//` comment between the links of a fluent
chain (`builder.Services.AddHealthChecks()`, `// Add a default liveness check`, `.AddCheck(...)`), which splits the
chain. StyleCop 1.2 reports it the same way. Keep SA1515's behavior, or exempt comments between chain links?

## Choices

- Keep SA1515's behavior: the blank line goes in, as StyleCop's fix does.
- Don't require it inside chains: a `//` comment whose next token is the `.` or `?.` of a member access needs no blank
  line above. A deviation that only makes StyleBro quieter, so migrated StyleCop-clean repositories stay clean.

## Decision

The owner: "Don't require it inside chains." Documented on the rule page and in differences-from-stylecop.md; the parity
set `blank-lines-comments` has the case (`Chains.cs`).
