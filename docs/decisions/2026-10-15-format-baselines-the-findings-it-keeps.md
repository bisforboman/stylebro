# format baselines the findings it keeps (2026-10-15)

## Question

Trial 23 (jint, `TreatWarningsAsErrors`): `stylebro-migrate format` ended clean with 227 findings its fixes keep on
purpose (renames that would break reflection, generated code, ...), and the build then failed with 369 errors. What
should `format` do with kept findings when warnings are errors?

## Choices

1. **Baseline them automatically**: when a run ends clean with kept findings, write them into the baseline the build
   reads (the existing `stylebro.baseline` mechanism: file format, location rules, `<StyleBroBaseline>`), say so, and keep
   the build and `--verify-no-changes` in agreement; new violations still fail.
2. List them and leave it to the user (suppress each, or run `stylebro-migrate baseline`), as before.
3. Fail the run when warnings are errors and findings are kept.

## Decision

**Choice 1** (owner's choice 2026-10-15). The baseline is the file `<StyleBroBaseline>` names, else the nearest
`stylebro.baseline` at or above the folder format runs on, else a new one there. Its other entries stay unless the line
they fingerprint is gone; `<StyleBroBaseline>none</StyleBroBaseline>` turns it off. Only a clean ending writes it.
