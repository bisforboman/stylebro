# Generator projects' Release-only code is fixed in a Release run (2026-10-17)

## Question

A project that takes an in-solution source generator stayed in its default configuration: `format` built the generator
for that one only, so its `#if !DEBUG` code was never fixed, and `--verify-no-changes` only built it in Release at the end
and reported what it found there (trial 28, ZLogger's Benchmark: 18 BRO1002 in Release-only code, "fix them by hand").
How should `format` treat such projects?

## Choices

1. **Fix it in a Release run**: build the in-solution generator in Release (each configuration a run needs) first, then
   give the project its Release run like any other project, so `format` fixes the Release-only code; verify keeps its
   Release build check.
2. Keep the default configuration only and leave the Release-only findings to the user (as before).
3. Only document it.

## Decision

**Choice 1** (owner's choice 2026-10-17: "Fix it in a Release run"). Such projects get the configuration runs other
projects get (where their code compiles differently there); before them `format` builds each generator the runs load in
that configuration (`Generators: building Gen first (in Release, ...)`), and verify's copy gets every configuration's
generator output. `--verify-no-changes` still builds the project in its other configurations at the end (`Building App in
Release to check it ...`).
