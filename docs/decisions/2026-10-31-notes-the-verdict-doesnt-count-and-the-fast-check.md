# Notes the verdict doesn't count and the fast check (2026-10-31)

## Question

Trial 64, TNG/ArchUnitNET: after a clean `bro format`, `bro check` answers from format's record (#312) only when the run's
"Not checked" notes are the held-back SDK ones (IDE0049). ArchUnitNET's "Not checked: xunit.analyzers' rules: ... no Fix
All ... (a note: the verdict doesn't count them)" kept the record from being marked checked, so every `bro check` ran all
its checks again: 82 s instead of about 1 s, with the same verdict. Should such notes also let check answer from the record?

## Choices

- Backlog it: a backlog item, this trial's PR keeps to its fix (recommended).
- Fix it in this PR: accept any note the verdict doesn't count in the record, with tests.
- Leave as is: only the held-back SDK note qualifies, on purpose.

## Decision

Backlog it (owner, 2026-10-31): `scripts/backlog/work/077-fast-check-with-notes.json`.
