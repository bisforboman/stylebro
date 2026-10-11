# Redundant `!` and `HasValue` (newer-rules survey #5, #6) (2026-10-05)

## Question

The survey of newer analyzer rules found two candidates: a redundant null-forgiving `!` (Sonar S8969/S8970) and
`x.HasValue` -> `x is not null` (Meziantou MA0171). Build them, and on by default?

## Choices

1. Both, off by default (also in the preset), off after `stylebro-migrate`.
2. Both on in the preset.
3. Leave the `!` to the SDK's IDE0370.

## Decision

The owner: both, off by default (choice 1). Agent's choices: BRO1147 decides per `!` from the operand's flow state
without the `!` (a speculative copy of the enclosing statement; the operand's own type info reports the state after the
`!`), skips `null!`/`default!`, types with type arguments, arrays, tuples and type parameters, ref/out arguments,
`#nullable` changing the project's setting, and projects with several target frameworks (`StyleBroTargetFrameworks`,
like the modernize guard). Found while building it: IDE0370 does work in SDK 10 builds and `dotnet format` when its
severity is set explicitly (the survey's probe hadn't), all-or-nothing per member; the rule page compares the two.
BRO1148 writes BRO1133's preferred form (`stylebro_null_check_style`) so the two converge in one run, skips the places
BRO1133 skips, and judges `(x.HasValue)` without its parentheses while BRO1405 is on (else the result depended on the
fix order).
