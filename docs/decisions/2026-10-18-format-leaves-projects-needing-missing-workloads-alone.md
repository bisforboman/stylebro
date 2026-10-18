# Format leaves projects needing missing workloads alone (2026-10-18)

## Question

Trial 30, BAndysc/AvaloniaVisualBasic6: the solution lists Android, iOS and Browser heads, and their workloads aren't
installed on the trial machine (NETSDK1147). `stylebro-migrate format`, `--verify-no-changes` and the preview stopped at
the restore ("The restore failed"), so nothing was formatted, although the code those heads share (AvaloniaVisualBasic,
Runtime, Desktop, Standalone) builds. A plain `dotnet format` on the solution fails the same way. What should format do?

## Choices

- Leave them out: projects whose evaluation lists missing workload packs (the SDK's `MissingWorkloadPack` items) are
  left out of the restore and the runs, and listed under "Leaving alone" with the packs and `dotnet workload restore`;
  the rest is formatted. Their own files stay unformatted until a machine with the workloads runs `format`.
- Keep stopping, with a better message (which projects need which workloads, a solution filter without them).
- Run `dotnet workload restore` automatically (often needs admin rights, downloads gigabytes).

## Decision

Leave them out (owner, recommended option).
