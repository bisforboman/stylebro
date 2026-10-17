# Format engine: an in-process host in stylebro-migrate (2026-10-17)

## Question

`stylebro-migrate format` takes 10-50x a build because it starts a `dotnet format` process per plan entry (framework,
configuration, shared-file part) and pass, three times per run (check, fix, re-check), each loading the workspace and
compiling everything again (CliFx: 24 processes for 1.5-3 s of analysis each). What should run the checks and fixes
([proposal](https://github.com/bisforboman/stylebro/blob/main/docs/proposals/format-speed.md), Q-D1)?

## Choices

1. **An in-process host in stylebro-migrate**: one workspace per configuration for the whole `format`, kept for the
   check, every fix, every run and the re-check. The largest gain (CliFx ~3-4x, PalCalc ~2-2.5x, re-checks ~10x); a large
   change that re-does what `dotnet format` does. CliFx: 24 processes before, 1 after.
2. A long-lived host process per plan entry, reused for check, fix and re-check: a smaller change (keeps the
   environment-variable hook and the parts), about half the gain.
3. Keep `dotnet format`, quick wins only: no risk to results; first runs stay ~4x a plain `dotnet format`.

## Decision

**Choice 1** (owner's choice 2026-10-17, with all six recommended options of the proposal). It comes in phases: phase 0
(quick wins in today's engine), phase 1 (the host for checks), phase 2 (the host for fixes, behind `--engine host`, see
[the rollout](2026-10-17-the-format-host-ships-behind-engine-host-first.md)).
