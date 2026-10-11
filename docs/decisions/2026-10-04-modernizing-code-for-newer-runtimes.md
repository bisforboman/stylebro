# Modernizing code for newer runtimes (2026-10-04)

## Question

Can StyleBro make code "nicer for newer runtimes" (target-typed `new`, collection expressions, `ThrowIfNull`, ranges,
`System.Threading.Lock`, ...)? A survey found a Fix All fixer in the SDK for every important rewrite, so no StyleBro
rule is needed. The SDK lacks one thing: in a multi-targeted project the rules fire per target framework and
`dotnet format` writes the newer framework's edit into the shared file. Probed: 14 of 15 API rules broke the net48
build (CA1847 silently bound `Contains(char)` to LINQ there), and 15 of 18 language rules broke it unless LangVersion
was set explicitly.

## Choices

1. `stylebro-migrate init --modernize`: an opt-in block of the SDK's rules in tiers, at `suggestion` where they aren't
   safe in the repository's multi-targeted projects.
2. A multi-target guard: a `DiagnosticSuppressor` that knows all of a project's target frameworks and hides the API
   rules an older one lacks, so every tier works in multi-targeted repositories.
3. Backlog only.

## Decision

The owner: `init --modernize` now ([modernizing.md](../modernizing.md)); the guard suppressor not now (backlog, Maybe).
The tiers: older C# (A) at warning everywhere; newer C# (B) at warning unless a multi-targeted project leaves
LangVersion unset; newer APIs (C) at warning only without multi-targeted projects. Debated rules (IDE0290, IDE0066,
IDE0063, IDE0305) and IDE0251 are left to the user; rules without a `dotnet format` fix are skipped.
