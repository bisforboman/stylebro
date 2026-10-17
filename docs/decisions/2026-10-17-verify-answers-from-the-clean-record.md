# Verify answers from the clean record (2026-10-17)

## Question

A clean `stylebro-migrate format` records the findings it kept, keyed by a hash of every C#, project and settings file,
the options and the tool's version. `--verify-no-changes` right after it only used that record to skip formatting a copy,
after running every check again (PalCalc 191 s, CliFx 160 s). Should it answer from the record without running anything
([proposal](https://github.com/bisforboman/stylebro/blob/main/docs/proposals/format-speed.md), Q-D5)?

## Choices

1. **Answer `--verify-no-changes` from the record when the state matches**: before PalCalc 191 s, CliFx 160 s after a
   clean `format`; after a few seconds (hashing the files). The key gains the SDK, package and tool versions; what it
   still can't see (an environment variable changing MSBuild evaluation) could make it say clean wrongly. CI (fresh
   checkout, no record) is unchanged.
2. Keep running the checks (today): verify always re-derives the answer.

## Decision

**Choice 1** (owner's choice 2026-10-17). Implemented in phase 0: the key also holds the SDK version (`dotnet --version`
in the folder), the StyleBro.Analyzers versions in the projects' restore output, and the tool's version (`dotnet format`
ships with the SDK, so the SDK version names it). The record answers only when the run that ended clean was a check of
every plan entry (what verify runs) with nothing but kept findings, no project skipped; otherwise verify runs as before.
The builds of the configurations an in-solution generator keeps from the checks still run: they are the check there.
