# bro format skips the xUnit fixes on xunit.analyzers 1.24 and later (2026-10-19)

## Question

Trial 33 (Tmds.DBus, xunit.v3 3.2.2 with xunit.analyzers 1.27.0): none of xunit.analyzers' code fixes supports Fix All
from 1.24.0 on (1.18.0-1.23.0 have it on every fix; checked by loading each version's fix providers and asking
`GetFixAllProvider()`). `dotnet format` applies fixes only through Fix All, so it printed "Unable to fix xUnit2013. Code
fix AssertEqualShouldNotBeUsedForCollectionSizeCheckFixer doesn't support Fix All in Solution"; `bro format` then said
"Clean" while `bro check` said "Not clean: 28 findings above no fix changes". What should bro do with xunit.analyzers
1.24 and later?

## Choices

1. **Skip the ids** (owner's pick): add the xUnit allowlist to `--diagnostics` only where the restore output has
   xunit.analyzers below 1.24, and say once why it isn't applied otherwise. `format` and `check` agree; the build still
   reports the warnings.
2. Keep the allowlist everywhere and treat "Unable to fix xUnitNNNN" as a failed fix (format says Not clean, like check).
3. Both.

## Decision

Skip the ids. `bro format` (and check, preview, baseline) adds the xUnit allowlist only for repositories whose restore
output (`obj/project.assets.json`) has xunit.analyzers below 1.24.0; with 1.24.0 or later anywhere it prints one line
saying xunit.analyzers' fixes can't run under `dotnet format` from that version on.
