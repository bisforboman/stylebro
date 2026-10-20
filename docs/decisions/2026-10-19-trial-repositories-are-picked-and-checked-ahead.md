# Trial repositories are picked and checked ahead (2026-10-19)

## Question

Each trial spent its first hour finding repositories that are unused, build on the trial machine and have offline tests,
and the random pick's list was rebuilt every trial (so `Random(<trial>)` couldn't be rerun). Should the candidates be
listed and checked beforehand, so another agent can double-check them?

## Choices

1. **A pool plus a fixed random list** (owner's pick): one file per candidate under `scripts/trials/pool/`, tagged by what
   it exercises, with build/test facts and who checked it; a committed `random.json` that is only appended to; `used.tsv`
   with every repository used or rejected so far.
2. Fixed lists of four repositories per trial for the next few trials (the targeted picks couldn't follow the previous
   trial's fixes).
3. One `candidates.psd1` file (every trial edits it: conflicts).

Who checks the entries: **a one-off cloud session now** (owner's pick), the daily triage session, or each trial itself.

## Decision

Pool, random list and used list in `scripts/trials/` (README there); a separate session checks the seeded entries in the
same PR. Trials still pick their targeted repositories by what the last fixes touched, now from the pool's tags first.
