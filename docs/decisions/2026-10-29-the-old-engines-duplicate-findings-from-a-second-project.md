# The old engine's duplicate findings from a second project instance are an allowed soak difference (2026-10-29)

## Question

Trial 60 engine soak, round 2 (after the fixes): trees, exit codes and verdicts were identical on both engines for
apache/logging-log4net and dotnetcore/EasyCaching, but the old engine's `bro check` on the unformatted code listed more
finding lines. log4net: 3 extra (BRO1606/BRO1504 at the line before the imports fix, next to the same finding one line up);
EasyCaching: 39 extra IDE0055 lines in EasyCaching.Core, at positions the net8.0 run already reports as WHITESPACE. Each came
from a run that loaded a second instance of a multi-targeted referenced project; the host's view loads one instance per
project (the trial 47/55 design) and doesn't report them. How should the soak treat this?

## Choices

- **Allowed difference** (recommended): documented with the other allowed differences; the finding lines are compared as
  sets of distinct findings, and the soak is clean if nothing else differs.
- A real difference: the soak is not clean; a follow-up makes the engines list the same lines.

## Decision

Allowed difference (owner, 2026-10-29).
