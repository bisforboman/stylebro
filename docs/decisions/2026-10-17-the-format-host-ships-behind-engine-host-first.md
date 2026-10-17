# The format host ships behind --engine host first (2026-10-17)

## Question

How should the in-process host ([engine decision](2026-10-17-format-engine-an-in-process-host-in-stylebro-migrate.md))
reach users ([proposal](https://github.com/bisforboman/stylebro/blob/main/docs/proposals/format-speed.md), Q-D6)?

## Choices

1. **Behind `--engine host`, the default only once a parity harness is byte-identical on every trial copy, the old
   engine kept for one release**: users see a change only once it is proven; costs keeping two engines for a release.
2. Switch directly in one release: faster to ship and one engine to maintain; any difference lands on users.

## Decision

**Choice 1** (owner's choice 2026-10-17). The parity harness compares the working trees after every run, the kept
findings, the exit codes, the baseline and the `--json` report of both engines on the trial repositories.
