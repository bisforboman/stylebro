# Readability batch B: eight rules beyond StyleCop (2026-10-05)

## Question

Which of these candidates (StyleCop issues #762 and #760, and survey #7, #8, #10, #12, #13 and #14 in
[beyond-stylecop.md](../beyond-stylecop.md)) should StyleBro build, and how should they be configured: `(x) => x`,
redundant `return;`/`yield break;`, unneeded `$`/`@`/raw strings, `else if`, empty record bodies, object creation
parentheses, one local per declaration, redundant base types?

## Choices

1. Build them all, on in the preset (`warning`), off after `stylebro-migrate` like the other rules beyond StyleCop.
2. Build them off by default (opt-in, like BRO1310).
