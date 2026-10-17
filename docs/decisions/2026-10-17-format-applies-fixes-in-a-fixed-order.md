# Format applies fixes in a fixed order (2026-10-17)

## Question

`dotnet format` applies one Fix All per diagnostic id, in an order that changes between processes, so two runs of the
same repository can apply the ids in different orders. In which order should the in-process host
([engine decision](2026-10-17-format-engine-an-in-process-host-in-stylebro-migrate.md)) apply fixes
([proposal](https://github.com/bisforboman/stylebro/blob/main/docs/proposals/format-speed.md), Q-D2)?

## Choices

1. **A fixed order**: deterministic runs; `FixOrderTests` already requires the same text in every order for interacting
   ids, and a difference the parity runs find becomes a test case. Before: two runs may apply ids in different orders;
   after: always the same order.
2. Mimic `dotnet format`'s order: closer to today, but that order isn't stable, so "byte-identical" can't be promised
   either way, and the host would have to reproduce an implementation detail of every SDK.

## Decision

**Choice 1** (owner's choice 2026-10-17).
