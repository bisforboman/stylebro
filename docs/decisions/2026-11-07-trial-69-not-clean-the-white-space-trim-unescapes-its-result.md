# Trial 69 not clean; the white-space trim unescapes its result (2026-11-07)

## Question

Trial 69's targeted repository, genaray/Arch, has a library with `<TargetFrameworks>net8.0; net6.0; netstandard2.1</TargetFrameworks>`. Trial 68's trim of white space in that list (backlog 082) returned it from a property function, which MSBuild escapes, so the SDK's inner builds became one ('TargetFramework=net8.0;net6.0;netstandard2.1') and the three projects that reference the library didn't load on the host (Incomplete, Not verified, exit 3). How should it be fixed, and is the trial clean?

## Choices

- Fix: unescape the trimmed list (with a fixture), drop the trim again, or trim only line breaks.
- Verdict: not clean (tally 0), or clean (the regression never shipped, and the tool said Not verified rather than a wrong Clean).

## Decision

Owner (2026-11-07): unescape the trim's result, with a fixture (`tfm-with-spaces-referenced`), a unit test and a mutation entry (backlog 084 Done); the trial is "Not clean" (tally 0).
