# The host falls back to dotnet format when its workspace load fails (2026-10-24)

## Question

Trial 50, ncalc: `--engine host` opens the whole solution once per configuration. The MSBuildWorkspace crash above (a file
that is AdditionalFiles in one project and Compile in another) made that load fail, so every run failed (0 files changed,
59 on the default engine). What should the host do?

## Choices

1. **Fall back to dotnet format** (owner's pick): when the host's solution load fails, the command runs on the default
   engine, with an `Engine:` line saying why (like the SDK 8 fallback of trial 49).
2. Load the projects one by one (OpenProjectAsync works there).

## Decision

Fall back to dotnet format.
