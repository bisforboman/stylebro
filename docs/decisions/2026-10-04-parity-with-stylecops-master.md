# Parity with StyleCop's master (2026-10-04)

## Question

StyleCop's unreleased `master` has ~60 commits since 1.2.0-beta.556 that change rules StyleBro replaces (a parity run
against a locally built `master` package confirmed them). Follow `master` everywhere, or only where it fixes a gap?

## Choices

For each change: follow `master`, or keep StyleBro's (beta.556-like) behavior and document the difference. The clear
gaps: BRO1104 `new nint()` (beta's `nint.Zero` doesn't compile before .NET 7), BRO1604/BRO1605 `init` accessors (and
the `Gets or sets or initializes` fix), BRO1611 primary constructor `<param>` tags, BRO1401 property patterns; also
BRO1606/BRO1607 missed multi-line whitespace-only summaries, which StyleCop reports. Changes that make `master` report
less: BRO1505 two single-line properties without a blank line (2aeb4e3d), BRO1601 explicit interface implementations (2959cac8), BRO1606 any summary starting
with `<para>` accepted without checking its text (29514d23); BRO1311 skipping overrides/implementations (d1b6183d) was
already a documented difference.

## Decision

The owner: the clear gaps follow `master`; StyleBro keeps its behavior for BRO1606 (`<para>` text checked), documented
on the rule pages and in [differences-from-stylecop.md](../differences-from-stylecop.md). First also kept for BRO1505 and
BRO1601; after seeing code examples the owner changed both to follow `master` (same day): two single-line properties
may sit together, and explicit interface implementations get no `<inheritdoc/>`. The parity sets stay on beta.556;
`Compare-WithStyleCop.ps1 -StyleCopFeed` runs them against a local `master` build when needed (no CI job).
