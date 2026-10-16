# Verify builds generator projects in their other configurations (2026-10-16)

## Question

`format` runs a project that takes an in-solution source generator in its default configuration only (#230: the generator
is built for that one only). Trial 26: `--verify-no-changes` said clean while `dotnet build -c Release` failed on BRO1509
and BRO1106 in Release-only code of such a project. What should verify do?

## Choices

1. Keep skipping the Release run in `format`, but have `--verify-no-changes` report the skipped code: build those projects
   in their other configurations and fail only if that build has findings.
2. Build the generator in every configuration and run format there too.
3. Only say that the code wasn't checked.

## Decision

**Choice 1** (owner's choice, 2026-10-16). `--verify-no-changes` says "Not checked by format: UsesGen in Release (it takes
an in-solution source generator): building it...", builds the project (`dotnet build -c Release --no-restore
--no-incremental`), lists the findings of format's rules in that project ("Release build of UsesGen: 2 findings") and is
not clean when there are any. Findings kept on purpose are in stylebro.baseline after a clean `format`, so the build
doesn't report them.
