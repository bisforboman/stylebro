### Changed

- **A StyleCop migration applies only where StyleCop ran:** `stylebro-migrate --write` finds the projects that
  reference StyleCop.Analyzers (directly or through Directory.Build.props/.targets or Directory.Packages.props) and
  writes settings for their folders only. Folders without StyleCop are listed in the report (`init` can set them up
  later) and `format` leaves them alone; sibling folders that share a name start are written as one wildcard.
- **Per-project StyleCop settings:** each project's own ruleset and `stylecop.json` are read; projects that differ from
  the rest get their own `.editorconfig` section instead of one project's settings winning.
- `--write` also writes `end_of_line` (as `init` does), and turns the preset off only for the projects StyleCop runs in.
- When the settings turn IDE0055 off, `format` skips the whitespace pass.
- Sonar S4136 maps to `stylebro_keep_overloads_together = same_access`.

### Fixed

- **File headers with `;` or `#`** (licence notices) were cut off: the header settings are escaped now and BRO1615 reads
  them back. IDE0073's template can't hold such text, so it stays off with a reason.
- **Central package management in the preview:** the version goes into the nearest Directory.Packages.props, a
  `GlobalPackageReference` next to StyleCop's (NU1008 before), and `<PackageReference Remove="StyleCop.Analyzers"/>`
  gets a matching remove for StyleBro.
- Files with old Mac line endings (a lone CR, which the SDK mishandles) are named and left out of the run.
- A failed restore names the NuGet error and points to `dotnet restore`; `--no-restore` works for multi-targeted
  repositories and in previews.
- Kept findings caused by a name in a string say where the string is; in a failing `format --verify-no-changes` they're
  marked `kept` with a note that they don't fail the run.
- `format --verify-no-changes` shows `dotnet format`'s real error instead of "formatting a copy failed".
- The preview keeps project files' byte order mark and adds `PackageVersion` entries in alphabetical order.
