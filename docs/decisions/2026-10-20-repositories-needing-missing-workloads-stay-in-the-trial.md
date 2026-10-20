# Repositories needing missing workloads stay in the trial pool, tagged (2026-10-20)

## Question

Trial 35, sqlite-net (pool entry, verified): its solution has projects targeting `net8.0-ios`, whose workload isn't
installed on the trial machine, and `bro format` stopped at the restore (NETSDK1147). Should such repositories stay in
the pool, and should this trial keep fixing format's stop on them? (Asked in the parent session.)

## Choices

1. **Keep them, tagged** (owner's pick): keep fixing format in this trial; pool entries whose solution needs a missing
   workload get the tag `needs-workload`, and a trial picks such a repository only when that is the lens.
2. Drop them from the pool, or reject them like repositories that need a missing SDK.

## Decision

Keep them in the pool, tagged `needs-workload` (sqlite-net; the other verified entries need none: ExcelDataReader's
`net10.0-windows` needs only `EnableWindowsTargeting`). The fix itself: [format leaves such a project
alone](2026-10-20-format-leaves-alone-a-multi-targeted-project-one-of-whose.md).
