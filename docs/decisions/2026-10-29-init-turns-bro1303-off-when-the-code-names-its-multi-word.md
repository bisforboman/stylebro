# init turns BRO1303 off when the code names its multi-word private fields snake_case (2026-10-29)

## Question

Trial 62, jbevain/mono.linq.expressions (no .editorconfig): all 11 of its multi-word private fields are snake_case
(`break_target`, `unique_names`), but init reported "private fields: 56 of 56 named 'field' -> camelCase" (it counted the
snake_case names as camelCase) and BRO1303 renamed all 11 to camelCase (StyleCop's SA1306/SA1310 would report them too).
Cecil (trial 61) kept its snake_case fields only because its .editorconfig has a snake_case naming rule. What should init
do when the code names its multi-word private fields snake_case?

## Choices

- **Detect it, BRO1303 off** (recommended): init counts multi-word private field names (snake_case against camelCase;
  single words don't tell); when snake_case is the code's convention, or the code mixes both, BRO1303 goes off and init's
  line says so, like BRO1503 in trial 61.
- Keep as is (SA1310 parity): BRO1303 renames them; only the init line's wording changes.
- A new snake_case field style: a third value for the private-field naming key, which BRO1303 enforces.

## Decision

Detect it, BRO1303 off (owner, 2026-10-29). The usual thresholds: 3 places or more, below 10 all must agree, from 10 on
75 %; mostly camelCase keeps BRO1303 on.
