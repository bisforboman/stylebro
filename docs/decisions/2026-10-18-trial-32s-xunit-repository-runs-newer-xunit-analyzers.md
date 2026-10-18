# Trial 32's xUnit repository runs newer xunit analyzers (2026-10-18)

## Question

Trial 32 was to take an xUnit-heavy repository with xUnit warnings left in it, to test `bro format`'s xUnit allowlist end
to end. A search of about 50 repositories found none that builds on the trial machine with real xUnit warnings:
maintained ones are clean, and the sloppy ones target dead frameworks or broken feeds (the best had 1 warning). How should
the third repository be tested?

## Choices

1. **Newer analyzers** (owner's pick): a buildable xunit 2.4.x repository with real assertion patterns, with
   xunit.analyzers 1.18 swapped in by the `CustomAfterMicrosoftCommonTargets` hook from backlog item 073's survey; then
   init, format, check, build and tests as usual. Logged as not the repository's own analyzer version.
2. A planted copy: assertion patterns written into a test project on a spot copy.
3. Three repositories this time.

## Decision

The trial uses an xunit 2.4.x repository with xunit.analyzers 1.18 swapped in for its analyzers, logged in the trial's
report as such.
