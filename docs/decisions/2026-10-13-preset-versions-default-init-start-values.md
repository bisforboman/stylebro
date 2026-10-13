# Preset versions: default, init, start, values (2026-10-13)

## Question

How preset versions work in detail ([new rules after 1.0](2026-10-11-new-rules-after-10.md) chose them; the design is
[proposals/preset-versions.md](../proposals/preset-versions.md)). Four open questions:

1. What an unset `StyleBroPreset` means.
2. Whether `stylebro-migrate init` writes the version.
3. When it starts: the next alpha with one layer, layers for 0.x minors now, or after 1.0.
4. Which values `StyleBroPreset` accepts.

## Choices

1. (A) The package's major (`1.0` in all of 1.x); (B) `latest`; (C) a build warning until it is set.
2. (A) The tool's newest preset version, in the root `Directory.Build.props`; (B) nothing; (C) `latest`.
3. (A) Ship the mechanism in 0.7.0-alpha.1 with one layer (`1.0` = every rule so far), layers per minor from 1.1;
   (B) layers for 0.x minors now; (C) build it after 1.0.
4. (A) Only `X.Y`, `latest` and `none`; (B) also `recommended` and an "installed" alias; (C) AnalysisLevel-style modes
   (`1.1-all`).

## Decision

All four (A), as recommended (owner, 2026-10-13). Also part of the design: rules added after 1.0 ship with
`isEnabledByDefault: false`, so they stay off without a preset that names them (`StyleBroPreset=none`, a migration's
block); `stylebro-migrate --write` keeps writing `none`. A version newer than the package warns (STYLEBRO002) and uses
the newest; anything else (`bogus`, `0.9`) fails the build (STYLEBRO001).
