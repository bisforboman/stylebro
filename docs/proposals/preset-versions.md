# Proposal: preset versions

Status: **decided and implemented 2026-10-13**: all four open questions as recommended below
([decision](../decisions/2026-10-13-preset-versions-default-init-start-values.md)), shipped in 0.7.0-alpha.1 with one
layer (`1.0`). Implements the decision of 2026-10-11 ([new rules after 1.0](../decisions/2026-10-11-new-rules-after-10.md)):
a minor release's new rules stay off until a team opts in, like .NET's `AnalysisLevel`. Differences from the text below:
the targets call the levels `_StyleBroPresetLevel`/`_StyleBroPresetLatest`/`_StyleBroPresetDefault`, the warning and
error are STYLEBRO002/STYLEBRO001, and the package check is verify-package's check 9.

## How it worked before

- The package's `build/StyleBro.Analyzers.targets` adds `stylebro.recommended.globalconfig` (`is_global = true`,
  `global_level = -1`) to `EditorConfigFiles` unless `<StyleBroPreset>none</StyleBroPreset>`. No other value is read;
  `recommended` is only the file name.
- The build, the IDE (design-time build) and `dotnet format` all load the project through MSBuild, so all three get
  the same `EditorConfigFiles`. `dotnet format`'s *style* pass ignores global-config severities, but that only matters
  for IDE rules (which is why `init` writes them into `.editorconfig`). **Probed today** (SDK 10.0.401): a rule with
  `isEnabledByDefault: false` (BRO1143) turned on only by `severity = warning` in a global config is reported by the
  build and fixed by plain `dotnet format`; without that line, neither.
- `init` leaves the preset on and writes only the IDE severities. `migrate --write` sets `StyleBroPreset=none` and
  writes a `dotnet_diagnostic.BROxxxx.severity` line for every rule it knows.
- **Gap:** every rule is `isEnabledByDefault: true`, so a rule added later reaches *every* repo at `warning`: through
  the preset, and also in migrated repos (`none`, and their block doesn't name it). Preset versions alone don't fix the
  migrated case.

## Proposed mechanism

1. **Rules added after 1.0 ship with `isEnabledByDefault: false`.** Without a preset that turns them on they are off,
   in every tool. This alone protects `none` users and migrated repos. A user can still turn one on by itself with
   `dotnet_diagnostic.BRO2001.severity = warning`.
2. **The version a rule was added in comes from `AnalyzerReleases.Shipped.md`** (its `## Release X.Y.Z` header; rules in
   `Unshipped.md` belong to the next minor). The release-tracking analyzer already forces every rule into these files,
   so no second table and no descriptor tag. Pre-1.0 headers (`0.1.0.N` etc.) all count as 1.0.
3. **Layered preset files, committed:** `build/presets/stylebro.1.0.globalconfig` (today's file, renamed),
   `stylebro.1.1.globalconfig` (only the rules new in 1.1 and their options), and so on. Each key appears in exactly
   one layer, so layers at the same `global_level` never conflict. A layer is a few lines; nothing is generated.
4. **The targets add every layer up to the chosen version**, one line per minor, at evaluation time (the packaging
   bug: `EditorConfigFiles` must be added there, not in a target):
   ```xml
   <EditorConfigFiles Include="$(MSBuildThisFileDirectory)presets/stylebro.1.1.globalconfig"
                      Condition="'$(StyleBroPreset)' != 'none' and $([MSBuild]::VersionGreaterThanOrEquals('$(_StyleBroLevel)', '1.1'))" />
   ```
   `_StyleBroLevel` is `StyleBroPreset` with `latest` replaced by the package's own major.minor and unset replaced by
   the default (question 1). A small target warns about a version newer than the package and errors on anything else.
5. **A rule off by default** (BRO1310, BRO1143, ...) keeps its `severity = none` line in its layer, as today. Opt-in
   and "held back" look the same in the descriptor; the preset layers tell them apart.
6. **At 2.0** all layers fold into a `2.0` base and the default moves to 2.0 (a major may change defaults).

## Open questions

### 1. What does an unset `StyleBroPreset` mean?

| Option | `.csproj` | After upgrading 1.0 -> 1.1 |
|---|---|---|
| **A. The package's major (`1.0` in all of 1.x)** | `<PackageReference Include="StyleBro.Analyzers" Version="1.1.0" />` | Build unchanged. 1.1's rules are off until `<StyleBroPreset>1.1</StyleBroPreset>` or `latest`. |
| B. `latest` | same | `warning BRO2001: ...` appears; only repos that pinned a version are safe. |
| C. Required: a build warning while unset | same | Build gets `warning STYLEBRO001: set StyleBroPreset`, but no new rules. |

**Recommended: A.** It is what `AnalysisLevel` does (defaults to the SDK's major) and the only option that meets
"upgrading never breaks a build by itself" for people who never read the docs; question 2 covers new users.

### 2. Does `init` write the version?

| Option | What `init --write` adds | New user installing 1.3 |
|---|---|---|
| **A. The tool's major.minor** | `<StyleBroPreset>1.3</StyleBroPreset>` in the root `Directory.Build.props` | Gets every rule up to 1.3 at once, pinned afterwards. |
| B. Nothing | - | Gets the 1.0 rules (question 1 A); has to know about `latest`. |
| C. `latest` | `<StyleBroPreset>latest</StyleBroPreset>` | Every rule now, and every future rule on upgrade (no protection). |

**Recommended: A.** A new setup is the right moment to take every rule, and the pin keeps later upgrades quiet.
`migrate --write` stays as it is: it writes `none` plus its own block, and new rules are off there through mechanism 1.

### 3. When does it start?

| Option | What 0.x users see |
|---|---|
| **A. Mechanism ships in the next alpha with one layer (`1.0` = everything so far); minors get layers from 1.1** | Nothing changes; `latest`/`1.0`/unset are identical until 1.1. |
| B. Layers for 0.x minors now (`0.7`, `0.8`, ...) | New alpha rules off until the trial repos raise the preset; the 0.x layers fold into 1.0 later. |
| C. Build it after 1.0 | 1.0 ships with all rules `isEnabledByDefault: true`; 1.1's rules must still default off, so the targets change in a minor. |

**Recommended: A.** verify-package and the trials exercise the targets before 1.0, without slowing alpha rule work
(versioning.md already says anything may change before 1.0).

### 4. Should `StyleBroPreset` accept more values?

| Option | Example |
|---|---|
| **A. Only `X.Y`, `latest`, `none`** | `<StyleBroPreset>recommended</StyleBroPreset>` -> build error naming the valid values. |
| B. Also `recommended` (= unset) and `installed` | `installed` can't work: the package doesn't know when it was installed; `init` writing the number covers it. |
| C. AnalysisLevel-style modes (`1.1-all`: off-by-default rules on too) | `<StyleBroPreset>1.1-all</StyleBroPreset>` |

**Recommended: A.** `recommended` was never a value anyone could set (the targets only test `none`), and no one has
asked for `-all`; add it when someone does.

## Tests and CI

- `PresetVersionTests` (next to `MigrationTests`): every rule's layer matches its release header in Shipped/Unshipped;
  every rule added after 1.0 has `isEnabledByDefault: false`; no key in two layers; every layer file has a line in the
  targets; the newest layer is newer than the last shipped release. The existing preset tests run over all layers.
- Test framework: `Verifier` and `DocExamplesTests` count a rule that the latest preset turns on as on (today they
  suppress off-by-default ids unless the test's `.editorconfig` sets them, which would hit every new rule).
- `verify-package.ps1` check 8: unset and `1.0` give the same findings; `latest` reports the newest layer's rule and
  `dotnet format` fixes it, the default doesn't; `StyleBroPreset=9.9` warns, `=bogus` errors. Until 1.1 exists the
  layer checks are skipped.
- StyleBro's own code, parity and real-world runs use `latest` (`Measure-MigrationDelta.ps1` points at the old file
  name and needs the layers).
