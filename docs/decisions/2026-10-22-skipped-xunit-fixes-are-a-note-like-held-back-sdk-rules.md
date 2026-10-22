# Skipped xUnit fixes are a note, like held-back SDK rules (2026-10-22)

## Question

Trial 44, charls-dotnet (xunit.v3 with xunit.analyzers 2.2.0, whose fixes have no Fix All): `bro preview` said Not verified
(exit 3, "Not checked: xunit.analyzers' rules"), while `bro format`/`bro check` on the real checkout said Clean (exit 0). The
skip was silent when the restore output already existed and became a gap only after a fresh restore, so the verdict depended
on restore state. There every xUnit project was in test/, which the migration leaves alone anyway. How should a skipped xUnit
allowlist count?

## Choices

1. **A note, like IDE0049** (owner's pick): always print the "Not checked: xunit.analyzers' rules" line (both paths) but leave
   it out of the verdict and exit code, like held-back SDK rules: the build still reports xUnit warnings.
2. Always a gap: format, check and preview all say Not verified wherever xunit.analyzers 1.24+ is restored.
3. Gap only where checked: like 2, but no line when every project referencing xunit is left alone.

## Decision

A note, like IDE0049.
