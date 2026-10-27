# Errors in a project the untouched build didn't compile are Not verified (2026-10-27)

## Question

Trial 57, DapperAOT: `bro check --build` after a `bro check` said "Not clean" (exit 2), and on the next run "Not verified"
(exit 3). It counted 7 CS0234 errors in Dapper.AOT.Test.Integration as new: that project depends on one that fails untouched
(RS1032), and after a design-time load MSBuild compiled it anyway without the failed reference, while the untouched HEAD
copy never compiled it ("Didn't compile"). How should errors in a project the untouched build didn't compile count?

## Choices

- **Not verified**: errors in projects the untouched build didn't compile have nothing to compare with: "Not verified",
  naming them (as the decision of 2026-10-20 does for projects that didn't compile), not "Not clean".
- Build the working tree in a fresh copy too (no bin/obj), so both builds start alike: slower.
- Leave as is (they stay new: Not clean).

## Decision

"Not verified" (owner, 2026-10-27).
