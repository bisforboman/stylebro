# check and format say "Not verified" (exit 3) for parts of the build they didn't check (2026-10-20)

## Question

An analysis of trials 1-35 found that every false "Clean" came from format/check seeing other compilations or other
file bytes than the build (a framework that didn't load, a workload skip, an unlisted multi-targeted project, files
excluded from a pass, xUnit or IDE ids held back, ...). Each such skip printed its own line, but the verdict still said
"Clean" (and check's exit code was 0, or 1 for projects that didn't load). What should check and format end with then,
and which skips count?

## Choices

Check:
1. **A "Not checked:" list (what, why) and "Not verified: ...; the rest is clean.", new exit code 3** (owner's pick),
   apart from 1 (an error) and 2 (findings). Today's "Not verified" (exit 1) moves to 3 too.
2. The same, exit code 1.
3. "Not clean", exit code 2.

Format (its last run changed nothing):
1. **The same list and line, exit code 3** (owner's pick).
2. The same, exit code 0.

Skips the build also makes (submodules, Visual Basic, generated and T4 files, files outside the repository, projects no
solution configuration builds, IDE0055 off):
1. **Not gaps, no list** (owner's pick): the build doesn't analyze them with StyleBro either; their lines stay as they are.
2. Listed as information. 3. Gaps too.

## Decision

Gaps make check and format say "Not verified" with a "Not checked:" list and exit with 3; deliberate skips the build also
makes aren't gaps.
