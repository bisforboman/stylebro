# Trial 68 not clean; a multi-line TargetFrameworks stays Not verified in the host (2026-11-05)

## Question

Trial 68 found CacheManager.Tests, whose `<TargetFrameworks>` spans three lines, left out of every host run (format and check said Clean, the build reported 460 findings). Fixed so it is Not checked (exit 3). The old engine rewrites TargetFrameworks per run and formats it. Should the host do the same? Should the check from format's record repeat the informational notes a full check prints? Is the trial clean?

## Choices

- Host: leave it Not verified (backlog 082), or format such projects too.
- Fast check: leave the notes out (backlog 083), or store and repeat them.

## Decision

Owner (2026-11-05, confirmed twice after two conflicting relays): make the host format such a project too (the injected targets file cuts white space out of TargetFrameworks; backlog 082 Done), leave the fast check as is (backlog 083), and the trial is "Not clean" (tally 0).
