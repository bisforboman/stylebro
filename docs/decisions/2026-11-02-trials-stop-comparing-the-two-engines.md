# Trials stop comparing the two engines (2026-11-02)

## Question

Every trial since the host became the default (2026-10-25) has run an engine soak: each repository on the host and on
`--engine dotnet-format`, compared byte for byte. The host has had nine clean soaks in a row (trials 52-64). Should trials
keep running the soak?

## Choices

- **Drop the soak** (recommended): trials run each repository on the default engine (the host) only and say whether it fell
  back to `dotnet format` and why; the old engine stays available as `--engine dotnet-format` for at least one release
  (#303); `scripts/format-speed/Compare-Format.ps1` stays for targeted checks.
- Keep the soak until the next release.

## Decision

Drop the soak (owner, 2026-11-02, asked in trial 65 through AskUserQuestion after the parent session passed the decision on).
Trial 65 is the first trial without it; its verdict asks only whether the trial is clean.
