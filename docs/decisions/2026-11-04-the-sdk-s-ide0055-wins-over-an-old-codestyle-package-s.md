# The SDK's IDE0055 wins over an old CodeStyle package's (2026-11-04)

## Question

Backlog 079 (moq4, trial 67): projects reference Microsoft.CodeAnalysis.CSharp.CodeStyle 3.8.0. `dotnet format`'s analyzer pass ran that
package's IDE0055 fix besides the SDK's; it does not know `{ A.B.Count: 1 }` (C# 10) or `extension(x)` (C# 14) and wrote `Count : 1` and
`extension (x)`, which the SDK-based check reports again. With the analyzer pass leaving the IDE ids to the SDK, format and check are
Clean, but the package's own IDE0055 analyzer in a warnings-as-errors build now reports those spots (17 in moq4): no code form satisfies both.

## Choices

1. SDK wins, and say it: one note naming the problem; `check --build` stays Not clean with the build's errors (recommended).
2. SDK wins, and the build's errors are a Not checked gap (exit 3).
3. The build wins: keep running the package's fixers.

## Decision

Owner (2026-11-04, AskUserQuestion): choice 1. Format and check follow the SDK; the analyzer pass gets no IDE ids when a project references
the package; format and check print `Old CodeStyle package: ...` once (remove the reference or update it).
