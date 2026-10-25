# The format host becomes the default engine (2026-10-25)

## Question

The format host (`--engine host`, StyleBro.FormatHost) had three clean soaks in a row on its current code (trials 52-54):
byte for byte the same trees, exit codes and `--json` as the default engine; a first `bro format` was 1.8-3.4x faster on
the six parity repositories (format-engine.md). The earlier decision ([ships behind --engine host first](2026-10-17-the-format-host-ships-behind-engine-host-first.md),
"the default after about 3 clean soaks", [2026-10-23](2026-10-23-a-host-only-difference-makes-a-trials-soak-not-clean.md))
left the switch to the owner. Does the host become the default for `bro format`, `bro check` and the previews, and what
happens to the old engine?

## Choices

1. **The host becomes the default** (owner's pick): `--engine dotnet-format` keeps the old engine as an explicit fallback for
   at least one release; the trials keep comparing both engines until the next release.
2. Keep the host behind `--engine host` for more soaks.

## Decision

The host is the default engine for `bro format`, `bro check` and the previews (owner, 2026-10-25). The old engine stays as
`--engine dotnet-format` (the value it already had, so scripts that name it keep working; `--engine host` still works too)
for at least one release. Where the host can't run or can't load the workspace it falls back to `dotnet format`, as
before, with an `Engine:` line. The parity harness (`scripts/format-speed/Compare-Format.ps1`) names both engines, and
each trial's soak keeps running it until the next release.
