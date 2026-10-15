# One-line empty blocks and IDE0055 (2026-10-15)

## Question

Trial 23 (spot b8ab): `init` kept one-line empty blocks ("19 of 19 on one line -> true", `stylebro_allow_empty_single_line_blocks`),
but IDE0055 with `csharp_new_line_before_open_brace = all` split `else { }` and `lock (x) { }` into two lines. Write an SDK
option that keeps them, or stop claiming they're kept?

## Choices

1. Write `csharp_preserve_single_line_statements = true` with the empty blocks line (probed with `dotnet format whitespace`
   on SDK 10.0.402: it keeps `else { }`, `lock (x) { }`, `while (x) { }`, `if (x) { }`; `csharp_preserve_single_line_blocks`
   alone doesn't).
2. Keep the key and add a note that IDE0055 splits those after a statement.

## Decision

Choice 1 (agent's call; the trial 23 task asked for an option combination if one exists). The option also lets IDE0055
leave other one-line statements alone, which loosens formatting but changes no existing code. When the repository sets
`csharp_preserve_single_line_statements = false` itself, its value stays and `init` adds the note instead.
