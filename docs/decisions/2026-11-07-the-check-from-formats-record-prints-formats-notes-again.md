# The check from format's record prints format's notes again (2026-11-07)

## Question

Trial 68 (backlog 083) found that a `bro check` answered from the record of the last clean `bro format` gives the same
verdict and exit code as a full check, but leaves out the notes a full check prints about the repository: "Leaving
alone: ...", "Old CodeStyle package: ...", "Configurations: ...", "Without StyleBro: ..." and the workspace warnings.
On 2026-11-05 the owner chose to leave it (docs/decisions/2026-11-05-trial-68-not-clean-a-multi-line-targetframeworks-stays-not.md).

## Choices

- Leave it: format printed the notes just before, and CI has no record, so it runs a full check.
- Store the notes in the record and print them again.
- Decide later.

## Decision

Owner (2026-11-07, asked twice, confirmed it reverses 2026-11-05): print them again. Format's first run keeps the
header lines that describe the repository, plus the workspace warnings note (`IsRecordedNote`). It leaves out the ones
about the run itself: the rules it fixes, "Using", "Engine:", the line endings it converted and the generators it
builds. They go into the record as `#note` lines, and the check from the record prints them after its first line.
Backlog 083 is done. This replaces the fast-check part of the 2026-11-05 decision.
