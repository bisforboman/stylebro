# Trial 57: not clean, the host soak is clean (2026-10-27)

## Question

Trial 57 (Newtonsoft.Json.Bson, OpenAPI.NET, ErrorProne.NET random, seed 57; DapperAOT targeted) found these, all fixed in
its PR (#309):

- BRO1404 added `internal` to DapperAOT's embedded template behind `#if !DAPPERAOT_INTERNAL` / `file` / `#endif`: CS9052 in the
  generated code, one test failed.
- A depth-1 clone (and the preview's own copy) broke Nerdbank.GitVersioning's design-time build, so projects loaded without
  their settings: `format` then `check` said "Clean." with 22 files of findings left; the preview missed those files.
- `check --build` flipped between "Not clean" and "Not verified" on errors in a project the untouched build never compiled.
- On Newtonsoft.Json.Bson, `check --build` printed its "Not verified" verdict before the builds.

OpenAPI.NET and ErrorProne.NET were clean and honest. Engine soak: no fallback; trees, exit codes, verdicts, --json, builds
and tests identical on both engines on all four, in both rounds; the only output difference (DapperAOT round 2's check
--build naming Dapper.AOT.Test.Integration as not compared in the host clone) came from MSBuild's state after earlier loads,
with the same verdict. Is the trial clean, and is the soak clean?

## Choices

- Trial: **Not clean** (tally stays 0) / Count it clean (tally 1).
- Soak: **Clean soak** / Not a clean soak.

## Decision

Trial: "Not clean" (owner, 2026-10-27): the tally stays 0. Soak: "Clean soak": the second clean soak with the host as the
default engine.
