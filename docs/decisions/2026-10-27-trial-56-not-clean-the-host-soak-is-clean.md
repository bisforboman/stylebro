# Trial 56: not clean, the host soak is clean (2026-10-27)

## Question

Trial 56 (FluentFTP, FusionCache, NetEscapades.EnumGenerators random, seed 56; DynamicData targeted) found two bugs, both
fixed in its PR:

- DynamicData: `bro check` and `check --build` stopped at their first project query (MSB4186) once the tests had been
  built, so a user with a built checkout couldn't run format or check at all (now retried with an empty target, as trial
  42 did for MSB4184).
- FusionCache: BRO1117 left 8 test files with an argument at column 0, depending on fix order (the untouched code has
  `, token: ...` at column 0; check called that result clean).

EnumGenerators and FluentFTP had no findings; every warnings-as-errors build and test suite matched the untouched code.

The engine soak: no fallback to dotnet format. Before the fixes, FusionCache's trees differed between the engines (the
BRO1117 fix-order bug, not host-specific: in round 1's soak both engines left column 0) and EnumGenerators' load-warning note
sat after a different run's header (both engines listed the same warnings; the host prints its note when its one load
ends, which varies from run to run, so `Compare-Format.ps1` now compares that line as unordered). After the fixes all four
were identical on both engines. Is the trial clean, and is the soak clean?

## Choices

- Trial: **Not clean** (tally stays 0) / Count it clean (tally 1).
- Soak: **Clean soak** (no host-only difference) / Not a clean soak (any engine difference before the fixes counts,
  decision of 2026-10-23).

## Decision

Trial: "Not clean" (owner, 2026-10-27): the tally stays 0. Soak: "Clean soak": the first clean soak with the host as the
default engine.
