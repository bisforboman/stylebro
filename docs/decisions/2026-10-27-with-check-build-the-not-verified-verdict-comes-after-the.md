# With check --build the Not-verified verdict comes after the builds (2026-10-27)

## Question

Trial 57, Newtonsoft.Json.Bson (netstandard1.3 doesn't load in `dotnet format`: Not verified by design): in
`bro check --build` the "Not verified: ... or build it (bro check --build)" line came before the builds, and after six clean
builds the output ended with "Release|x86: no findings, no new errors." and exit 3, without a verdict at the end. Change it?

## Choices

- **Verdict last**: with --build the Not-verified verdict waits for the builds and comes last; it says the builds compiled
  that code with warnings as errors (StyleBro's rules at warning were checked there), what they don't check (whitespace and
  IDE fixes without EnforceCodeStyleInBuild), and drops the 'bro check --build' hint.
- Clean when the builds pass and EnforceCodeStyleInBuild is on everywhere.
- Leave as is.

## Decision

"Verdict last" (owner, 2026-10-27).
