# BRO1401 in nested property patterns (2026-10-11)

## Question

A trial on microsoft/vs-validation (StyleCop.Analyzers.Unstable 1.2.0-beta, clean): BRO1401 added trailing commas inside
nested multi-line property patterns (`{ Identifier.ValueText: "Requires" },` followed by `},` and `},`), because it
followed StyleCop's unreleased `master` (94671b70, decision of 2026-10-04). Keep following `master`, or the release?

## Choices

- Follow `master`: multi-line property patterns, nested ones too, get a trailing comma.
- Follow released StyleCop 1.2.0-beta.556: property patterns aren't checked (the parity set `strings-commas` confirms
  beta.556 reports neither a pattern nor a nested one).

## Decision

The owner: "Follow released StyleCop 1.2." BRO1401 doesn't check property patterns; differences-from-stylecop.md lists
it with the `master` changes StyleBro doesn't follow.
