### New

- **`stylebro_allow_leading_underscore_in_pascal_fields`** (BRO1306): `true` leaves constants, `static readonly` and
  non-private fields that start with `_` alone, like StyleCop when SA1309 is off. `stylebro-migrate --write` sets it from
  SA1309.

### Changed

- **`stylebro-migrate format --verify-no-changes` fails (exit code 1) when a project wasn't loaded in any run**, instead
  of reporting "Clean." for code it never checked. Projects skipped for some target frameworks only, or because they
  reference an F# project, stay warnings.

### Fixed

- **`stylebro-migrate --write` no longer renames `_x` static readonly fields StyleCop accepted.** With SA1309 off,
  StyleCop's SA1311/SA1303 only check that the first letter isn't lower-case; BRO1306 renamed `_defaultSelector` to
  `DefaultSelector` and failed builds with `TreatWarningsAsErrors`.
- **A second `stylebro-migrate --write` after replacing StyleCop with StyleBro writes the same settings.** When StyleCop
  ran in some projects only, the second run counted every project and changed rules, field naming and file headers.
- **SDK-style projects with a leftover `packages.config`** are no longer treated as old-style: `--diff` adds the normal
  `PackageReference`, `format` restores them with `dotnet restore`, and the `Directory.Build.props` reference for
  mixed repositories now reaches them (`'$(UsingMicrosoftNETSdk)' == 'true' or !Exists(...packages.config)`).
- **`stylebro-migrate format` formats test projects that reference a single-target project** (for example a
  `netstandard2.1` helper referencing a multi-targeted library). The per-framework run couldn't load them before and
  said to run `dotnet restore`.
- **`--diff` stops with git's error** when a git command in its copy fails, instead of reporting "nothing to change". It
  also works with paths longer than 260 characters on Windows.
- **`--diff`'s settings summary** counts only real suppressions, not the lines that add the StyleBro.Analyzers
  reference, and **its samples** show a change once with every rule that made it.
- **A malformed solution filter (`.slnf`)** gives a clear error instead of a stack trace.

### Upgrading

- Migrated from StyleCop with SA1309 off? Run `stylebro-migrate --write` again: it adds
  `stylebro_allow_leading_underscore_in_pascal_fields = true`, so BRO1306 stops asking to rename `_x` static readonly
  fields.
