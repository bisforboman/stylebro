### New

- **Preset versions.** `<StyleBroPreset>` now takes a version (`1.0`, later `1.1`, ...) or `latest`, like .NET's
  `AnalysisLevel`: from 1.1 on, a release's new rules stay off until you raise it, so upgrading the package never adds
  warnings by itself. Unset means the package's major version (`1.0`); before 1.0 every rule is a 1.0 rule, so nothing
  changes yet. `stylebro-migrate init --write` pins the version in `Directory.Build.props`. A version newer than the
  package warns (STYLEBRO002); any other value than a version, `latest` or `none` fails the build (STYLEBRO001).
  See https://bisforboman.github.io/stylebro/versioning/#new-rules

### Changed

- The preset file moved from `build/stylebro.recommended.globalconfig` to `build/presets/stylebro.1.0.globalconfig` in
  the package. Only matters if your build references the file by path.
