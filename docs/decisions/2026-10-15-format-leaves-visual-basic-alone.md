# format leaves Visual Basic alone (2026-10-15)

## Question

Trial 23 (a solution with C# and Visual Basic projects): `stylebro-migrate format` reformatted and renamed in `.vb`
files without listing them, and `--verify-no-changes` reported Visual Basic findings as StyleBro's and exited 2. What
should format do with Visual Basic?

## Choices

1. **Leave VB alone**: format and verify run on the C# projects only and never touch or count `.vb` files; the docs say
   VB projects are left out.
2. Format VB files too (whitespace and the SDK's rules), and list them.

## Decision

**Choice 1** (owner's choice 2026-10-15). The runs load the C# projects (a VB project a C# project references still
loads, as a reference), format only their files (`--exclude **/*.vb` besides), and a `.vb` file a fix reaches anyway is
put back and named. The names in `.vb` files go into the repository-names file, so a C# member VB code uses keeps its
name (a kept finding).
