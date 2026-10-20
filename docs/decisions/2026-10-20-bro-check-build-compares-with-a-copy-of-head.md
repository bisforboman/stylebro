# bro check --build builds every configuration and compares with a copy of HEAD (2026-10-20)

## Question

Every false "Clean" of trials 1-35 was caught only by the trials' TreatWarningsAsErrors Debug/Release build-and-test
comparison, which check never did. Should check get a `--build` option that builds the configurations and frameworks
with TreatWarningsAsErrors and compares the errors with the untouched code, now or later, and against what?

## Choices

1. **Now, against a copy of git HEAD** (owner's pick): the solution's configurations (and platforms) are built with
   TreatWarningsAsErrors; a finding of format's rules always fails; any other error fails only when the build of a copy
   of HEAD (like preview's copy; the working tree is never touched) doesn't have it. Without git, any other error fails as
   "build failed". Tests aren't run.
2. Now, the working tree only: any error fails.
3. Later, as a backlog item.

## Decision

`bro check --build`, in this pull request, compared with a copy of HEAD.
