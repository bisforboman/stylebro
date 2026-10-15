# Renames leave Visual Basic code alone (2026-10-15)

## Question

Trial 23 (a C# library used from a VB project): BRO1309's rename of `tariffHelper` was kept with the reason "the new
name would mean something else": VB is case-insensitive, so the lookup at the VB use found the old name under the new
one. Other renames edited the VB files. Should the naming fixes edit Visual Basic code?

## Choices

1. **Leave VB alone**: a name that VB code uses keeps it, with the kept reason `UsedFromVisualBasic`.
2. Rename in VB code too (case-insensitive lookups need their own checks).

## Decision

**Choice 1** (owner's choice 2026-10-15: "Leave VB alone").
