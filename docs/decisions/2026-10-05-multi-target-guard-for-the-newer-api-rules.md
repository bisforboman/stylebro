# Multi-target guard for the newer-API rules (2026-10-05)

## Question

`init --modernize` wrote tier C (the SDK rules whose fixes need a newer API) as `suggestion` in any repository with a
multi-targeted project, because the rules report in the newer framework's compilation and `dotnet format` writes the
fix into the file the older framework compiles too. Build the guard suppressor the 2026-10-04 decision left on the
backlog?

## Choices

1. Build it: a `DiagnosticSuppressor` that knows all of a project's target frameworks and hides a tier C diagnostic
   when one of them lacks the API; tier C becomes `warning` everywhere.
2. Keep the `suggestion` fallback.

## Decision

The owner: build it ([modernizing.md](../modernizing.md#the-multi-target-guard)). Agent's choices: the package passes
`StyleBroTargetFrameworks` (`TargetFrameworks` with `,` for `;`, since `;` starts a comment in the generated
editorconfig); a hand-kept table of minimum .NET (Core) / .NET Standard versions per rule, set to the newest API the fix
can write (CA1872 .NET 9 for `ToHexStringLower`, CA1850 .NET 6 for HMAC, CA2263 .NET 5); unknown frameworks count as
lacking everything; single-target projects are never touched. `init --modernize` writes tier C at `warning` always, no
`suggestion` fallback: the guard ships in the same package as the preset `init` sets up, and a note says projects
without the package have no guard. A MigrationTests check keeps the table and tier C in sync.
