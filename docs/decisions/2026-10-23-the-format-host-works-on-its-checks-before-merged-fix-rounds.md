# The format host works on its checks before merged fix rounds (2026-10-23)

## Question

Phase 3 of the [format-speed proposal](https://github.com/bisforboman/stylebro/blob/main/docs/proposals/format-speed.md)
(merged fix rounds) was to come only if, after phase 2, the per-id fix loop was still the largest cost. Measured on main with
`bro format --engine host --timings` (4 cores, SDK 10.0.113), the per-id loop was the largest part only on Serilog (36 of
91 s). Elsewhere the checks were: CliFx 62 s with an 8 s fix phase, PalCalc 137 of 249 s, Newtonsoft.Json 635 of
1,012 s. Each fix run's first analysis also repeated what its check had just analyzed (Newtonsoft.Json 188 s). What next?

## Choices

1. **Work on the checks first**: reuse the check's analysis in the fix run and make the check after a run that changed
   files analyze only what changed; merged fix rounds later.
2. Build merged fix rounds anyway, behind their own switch.
3. Find out first why Newtonsoft.Json still changes after 3 runs.

And for how it ships, since trial 47 soaks `--engine host`:

1. **Straight into `--engine host`**, restarting the soak count (it is results-neutral by design).
2. Behind its own switch until the soak is done.

## Decision

**Checks first** and **straight into `--engine host`, restarting the soak** (owner's choices 2026-10-23). Merged fix rounds
would have shipped behind a switch of their own (owner's earlier answer); they wait.
