# check --build counts an emit-time compiler error as after the analyzers (2026-10-23)

## Question

Trial 49, datalust/superpower: the untouched code's warnings-as-errors build fails on `CSC : error CS8002` (a strong-named
benchmark project references the unsigned Sprache). The error has no place in the code, so `bro check --build` treated it
like a restore or MSBuild failure and said "Not verified: ... compiled nothing past their errors". But CS8002 comes from the
compiler's emit step, after the analyzers ran: a BRO1503 violation planted in that project was reported (and emit was then
skipped). Cocona's pre-existing CS0618, which has a place in the code, already gave Clean. How should such errors count?

## Choices

1. **Emit-step list** (owner's pick): location-less compiler errors known to come from the emit step (CS8002, probed) count
   like errors in the code; every other error without a place still means Not verified.
2. All CSC errors: any error MSBuild attributes to `CSC` counts as the compiler having run. Simpler, but command-line
   errors such as CS0006 (a referenced assembly missing) or CS2001 stop the compiler before the analyzers: a false Clean.
3. Keep Not verified and only reword the message.

## Decision

Emit-step list: `FormatCommand.EmitErrors` (CS8002) and `StopsCompiling`; superpower's check --build is Clean.
