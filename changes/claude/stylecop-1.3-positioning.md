### Changed

- **`bro migrate`** knows StyleCop.Analyzers 1.3: in a repository on a 1.3 beta the rules 1.3 added (SA1143, SA1315, SA1317, SA1653, SA1654, SX1412) count, and the report lists those nothing enforces yet; in a repository on 1.2 they count as off, as before.
- [Migrating](https://bisforboman.github.io/stylebro/migrating/#coming-from-stylecop-13) has a section for teams coming from StyleCop.Analyzers 1.3.

### Fixed

- **`bro migrate`** follows imports written with `$([MSBuild]::GetDirectoryNameOfFileAbove(...))` (OpenTelemetry's `src/` and `test/` import their build settings that way). Before, the rulesets behind them weren't read, so rules the repository turns off counted as on, and the migration turned their StyleBro rules on: in OpenTelemetry that added `/// <inheritdoc/>` to 170 files and removed blank lines after comments in 46.
