# One-line statements in `init` (2026-10-10)

## Question

The preset sets `csharp_preserve_single_line_statements = false`, so `dotnet format` split NSubstitute's 58 lines like
`if (a) return false;` while `init` reported "one-line bodies: 148 with braces, 93 without -> both allowed". When should
`init` keep statements on their owner's line?

## Choices

1. Keep them (`true`) once the code has 3 or more, majority or not.
2. Treat it like the other conventions (75 % from 10 places on).
3. Leave the preset's `false`.

## Answer

**Choice 1** (owner: "Keep them if the code has 3+"). `init` counts braceless `if`/`else`/loop/`using`/`lock` bodies and
the statements of a `case` section on the line of their owner (or the label) against those below it. With 3 or more on the
owner's line it writes `csharp_preserve_single_line_statements = true` with an `# init:` comment; a key the repository
sets stays.
