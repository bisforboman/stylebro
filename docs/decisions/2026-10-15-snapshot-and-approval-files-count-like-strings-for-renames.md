# Snapshot and approval files count like strings for renames (2026-10-15)

## Question

Trial 23 (xunit.v3 + Verify): BRO1309 renamed an internal DTO's `customerName` to `CustomerName`, and the snapshot
tests failed (15/15 -> 13/15): the `*.verified.txt` files hold the names the serializer wrote. The renamer's string
guard reads C# string literals only. Should checked-in snapshot and approval files keep a rename back too?

## Choices

1. **Treat them like strings**: the renamer reads `*.verified.*`, `*.received.*` and `*.approved.*` files (Verify,
   ApprovalTests) under each project's repository, and a data member (instance property or event, public or internal
   instance field, enum member) whose name is a word there keeps it, with the kept reason `NameInSnapshotFile`.
2. Leave them alone: the snapshot tests fail, and the user accepts the new snapshots.

## Decision

**Choice 1** (owner's choice 2026-10-15: "treated like strings"). Only those clear file-name patterns; no guessing at
test-data folders. Files under `bin`, `obj`, `.git`, `.vs` and `node_modules`, binary files and files over 1 MB are
skipped. The fix reads them (the analyzer can't read files), also for projects a `stylebro-migrate format` run doesn't
load, since it scans the repository from its git root.
