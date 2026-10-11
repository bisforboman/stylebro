# What `stylebro-migrate format` fixes (2026-10-09)

## Question

A trial of 0.2.0-alpha.1 on Kavita ran plain `dotnet format` as the docs said. Besides StyleBro's fixes it applied
every other analyzer's and the compiler's: CS8618's fix added `required`, a Sonar fix (S1144) removed `init;`
accessors, and the repository no longer built (9 errors). Should StyleBro's tooling limit what gets fixed?

```
# Before: the docs said 'dotnet format'; 'stylebro-migrate format' passed everything through
dotnet format                          # StyleBro + SDK + Sonar + compiler fixes

# After
stylebro-migrate format                # dotnet format --diagnostics BRO1001 ... IDE0055 IDE0036 ... (+ whitespace)
stylebro-migrate format --all          # everything, like before
```

## Choices

1. Patterns: extend BRO1405 (but then `stylebro-migrate`'s SA1119 -> BRO1405 mapping would turn on more than StyleCop
   checked), a new rule beyond StyleCop next to it, or leave patterns alone.
2. Fading: a hidden, not configurable companion like StyleCop's SA1119_p, or none (the warning alone).

## Decision

The owner: a new rule, [BRO1410](../rules/BRO1410.md), on in the preset and off after `stylebro-migrate` (like every rule
beyond StyleCop). It only removes parentheses whose removal parses to the same pattern, and leaves the ones
[BRO1407](../rules/BRO1407.md) (SA1408) wants between `and` and `or` while BRO1407 is on, so the two converge in one run in
any order. And fading like StyleCop: `BRO1405_p` and `BRO1410_p` (hidden, `Unnecessary` and `NotConfigurable` tags)
on the `(` and `)` of each finding, reported only where their rule is on, no fix of their own, and no baseline,
migration or rule-page entry of their own.

A survey of the 12 reference repositories found 32 parenthesized patterns and no finding: 24 put an `and` inside an
`or`, 8 follow `not`.
1. Limit `stylebro-migrate format` to StyleBro's ids and the SDK rules `init`/the migration turn on, `--all` for
   everything; the docs warn about plain `dotnet format` and show the `--diagnostics` equivalent.
2. Only warn in the docs; keep passing everything through.
3. Leave it to the user.

## Decision

The owner: "Limit format, warn in docs" (choice 1). Whitespace formatting still runs (it isn't filtered by
`--diagnostics`), and the docs now recommend `stylebro-migrate format` for every repository, not only multi-targeted
ones. In the same change `format` excludes git submodules (`--exclude`), since a project can compile files from one.
