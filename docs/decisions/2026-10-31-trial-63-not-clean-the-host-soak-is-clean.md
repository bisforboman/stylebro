# Trial 63: not clean, the host soak is clean (2026-10-31)

## Question

Trial 63 (serilog/serilog-sinks-file, tonerdo/pose, NLog/NLog.Extensions.Logging random, seed 63; mono/t4 targeted at trial
62's tab and encoding fixes) found these, all fixed in its PR:

- NLog.Extensions.Logging: BRO1126 dropped the `?` of `object?` when it qualified a using alias inside a namespace, and the
  build broke with CS8619/CS8624.
- mono/t4: BRO1505 added about 130 blank lines between neighbouring one-line members, which t4 groups (owner's decision:
  init writes `stylebro_allow_adjacent_single_line_members = true` from 3 such pairs).
- mono/t4: the first format of a fresh clone ran net7.0 on its own, later runs merged it with net6.0 (the plan reused an
  evaluation made before the restore; speed only; fixture `first-run-framework-merge`).

Held: trial 62's tab and encoding fixes (no space-indented lines in t4), #312's fast check matched the full check (record
deleted) and `check --build` on all four; in round 2 every TWAE build and test suite matched the untouched code. Engine
soak: both rounds identical on all four (trees, exit codes, verdicts, --json, logs, Compare-Format), no fallback. Is the
trial clean, and is the soak clean?

## Choices

- Trial: **Not clean** (tally stays 0) / Clean (tally 1).
- Soak: **Clean soak** / Not clean.

## Decision

Trial: "Not clean" (owner, 2026-10-31): the tally stays 0. Soak: "Clean soak": the eighth clean soak with the host as the
default engine.
