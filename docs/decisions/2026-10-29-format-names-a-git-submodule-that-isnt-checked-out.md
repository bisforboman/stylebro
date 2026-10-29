# Format names a git submodule that isn't checked out (2026-10-29)

## Question

Trial 60, khellang/Middleware: ProblemDetails compiles files from the git submodule `lib/AspNetCore`. Cloned without
`--recursive` (the submodule folder is empty), that project doesn't compile, yet `bro format` and `bro check` said clean
(exit 0); only `bro check --build` said Not verified (exit 3). The changes happened to equal the full checkout's. What
should format and check do when a submodule isn't checked out?

## Choices

- **Name it, leave out IDE0005** (recommended): like the `Missing:` note for missing compile files, name the submodule
  and how to get it (`git submodule update --init`), and leave out IDE0005, since `using` lines can look unused. Verdicts
  unchanged. A new fixture shape.
- Name it and say Not verified (exit 3), like `check --build`.
- Leave as is: `check --build` already reports it.

## Decision

Name it, leave out IDE0005 (owner, 2026-10-29).
