# Format leaves alone a multi-targeted project one of whose frameworks needs a missing workload (2026-10-20)

## Question

Trial 35, sqlite-net: `bro format` stopped at the restore (NETSDK1147). Two projects of SQLite.sln target
`netstandard2.0;net8.0;net8.0-ios`, and the iOS workload isn't installed. Trial 30's fix leaves a project alone when it
needs a missing workload, but it only caught single-target heads: a multi-targeted project's outer evaluation lists no
`MissingWorkloadPack` items; only the inner `net8.0-ios` evaluation does. What should format do with such a project?

## Choices

1. **Leave it alone** (owner's pick): evaluate each platform framework (`net8.0-ios`, `-android`, ...) too; if one needs a
   missing workload, leave the whole project out of the restore and the runs with trial 30's line. Files it shares with
   other projects (sqlite-net's src/SQLite.cs, also compiled by SQLite-net-base) are still formatted through those.
2. Format its other frameworks: format's MSBuild hook drops only the frameworks whose workloads are missing for that
   project. More code formatted, but the project is restored and loaded with frameworks its file doesn't say.

## Decision

Leave it alone.
