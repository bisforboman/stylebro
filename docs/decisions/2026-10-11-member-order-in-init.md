# Member order in init (2026-10-11)

## Question

BRO1001 reordered members in 407 files of StabilityMatrix and 167 of Flow.Launcher, repositories that never sorted their
members. Should `init` judge whether a repository's code is sorted?

## Choices

1. **Detect, like other conventions**: count the types already in BRO1001's order, apply the usual thresholds.
2. Keep BRO1001 on everywhere (StyleCop's default).
3. Turn BRO1001 off in `init` and leave it opt-in.

## Decision

**Choice 1** (owner: "Detect, like other conventions"). Agent's choices: the places are types whose members BRO1001
would sort apart (two or more members with different sort keys; types it leaves alone don't count), judged with
BRO1001's own key logic (`MemberOrdering.IsSorted`) and the repository's order settings. A type that is in order once
overloads stay together counts as sorted: the overloads convention decides that part. Only type members count, not the
types of a namespace: most files hold one type, and the unit stays the one BRO1001 reports. Mostly sorted keeps BRO1001
on; mostly unsorted or mixed writes `dotnet_diagnostic.BRO1001.severity = none` with an `# init:` comment, and a later
`init` keeps that line ("kept from the last init"). Measured: StabilityMatrix 370 sorted, 414 not; Flow.Launcher 107, 171;
Orleans 3041, 1990: off in all three.
