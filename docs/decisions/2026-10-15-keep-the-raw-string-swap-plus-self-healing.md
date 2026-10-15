# Keep the raw-string swap, plus self-healing (2026-10-15)

## Question

`stylebro-migrate format` protects lines starting with `#` inside multi-line strings in an `#if` branch (the SDK's
formatter moves them to column 0 as if they were directives, a Roslyn bug) by swapping their `#` for a private-use
character while `dotnet format` runs and putting it back afterwards, also on Ctrl+C (PR #210). A hard kill in between
leaves the swapped character in the user's file. Keep the swap, and if so, how to guard against that?

## Choices

1. **Keep the swap, plus self-healing**: before swapping, write a recovery record (files, characters, counts, the
   process); delete it after restoring. Every `stylebro-migrate` command first restores what a killed run's record names
   (only where the swapped characters are still at directive positions; otherwise say so and leave the file), and
   `format`/verify stop with a clear message on a swapped character no record explains.
2. Keep the swap as it is (Ctrl+C and normal exits are covered; a hard kill isn't).
3. Drop the swap and leave such files out of the whitespace pass (two more `dotnet format` runs per run that has them:
   CommunityToolkit 8 to 11.5 minutes).

## Decision

**Choice 1** (owner's choice 2026-10-15). The record is `%TEMP%/stylebro-mask/<hash of the root>-<process id>.json`; a
record whose process still runs (same id and start time) is left alone.
