# bro format holds back the guard's IDE fixes where dotnet format ignores suppressors (2026-10-20)

## Question

Trial 35: on the trial machine's SDK (10.0.113, Ubuntu's 1xx band) `dotnet format`'s style pass doesn't load
DiagnosticSuppressors for IDE rules, so the multi-target guard never hides IDE0049 or the tier-C IDE rules there
(`scripts/verify-package.ps1` fails check 5 here; CI passes on the newest SDK 10). sqlite-net's netstandard2.0 build broke
on `using X = nint;`. What should `bro format` do?

## Choices

1. **Hold those ids back itself** (owner's pick): probe once per `dotnet format` version (cached in the temp folder)
   whether it hides an IDE diagnostic a suppressor suppresses; where it doesn't, leave IDE0049 and the tier-C IDE rules
   (IDE0056, IDE0057, IDE0330) out of `--diagnostics` in repositories where the guard matters (multi-targeted projects or
   shared files with an older C#), with a "Held back" line. Builds and newer SDKs keep the guard as it is.
2. Run them in a `dotnet format` call of their own that excludes the files the guard covers (slower, more code).
3. Document only: the guard needs a recent SDK's `dotnet format`.

## Decision

Hold those ids back.
