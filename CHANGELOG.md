# Changelog

Versions are git tags (`vX.Y.Z`, prereleases `vX.Y.Z-alpha.N`); each one publishes StyleBro.Analyzers and
StyleBro.Migrate to nuget.org and a GitHub release with its section below as the notes. What a version number may
change: [docs/versioning.md](docs/versioning.md). What's merged but not released yet is in [changes/](changes/) (one
file per pull request).

## 0.5.0-alpha.1 – 2026-10-10

129 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### Fixed: problems in 0.4.0-alpha.1

- **Renames inside `#if` code broke builds:** a type parameter (or another name) used in a branch of `#if` that the
  current target framework doesn't compile was left with its old name. Names that appear in disabled code are no
  longer renamed.
- **`--diff` previewed without StyleBro** in repos whose projects use a nested `Directory.Build.props`: the package
  reference now goes where MSBuild picks it up (also for `--write`), and the preview warns when no StyleBro rule ran.

### New

- **Findings kept on purpose say why.** Renames StyleBro holds back (the name is read by reflection, used in
  `nameof`, public API, ...) get no code action, and `stylebro-migrate format` lists them with the reason.
- **A clean check that means clean:** `stylebro-migrate format --verify-no-changes` exits 0 when only kept findings
  remain (plain `dotnet format --verify-no-changes` keeps failing on them). Exit codes are in `--help` and the CI guide.
- **`init` explains what it adopted:** every convention gets a verdict (kept, default, off, too few), a summary says
  which rules were turned off because the code mixes both forms and how to choose later, and the written
  `.editorconfig` comments explain themselves.

### Changed

- **`init` needs at least 3 places** before following a style; with no clear majority it turns the matching rule off
  instead of rewriting the code to StyleBro's default.
- **SCREAMING_CASE locals** are converted word by word (`RANGE_REP_REG` → `rangeRepReg`).
- **BRO1150 with Sonar:** on lists and arrays it writes `Exists`/`Find` when the project's Sonar setup asks for them.

### Fixed

- `init`: vendored folders are treated as generated code; no hint for settings the repo already sets.
- `format`: names skipped projects, stops when restore is broken, lists the files each run changed.
- `--diff` describes invisible changes (byte order mark, final newline).
- BRO1606 no longer prefixes summaries that already start like a constructor sentence.

### Upgrading from 0.4.0-alpha.1

Bump the package, then update the tool:
`dotnet tool update -g StyleBro.Migrate --prerelease`.
Use `stylebro-migrate format --verify-no-changes` in CI instead of plain `dotnet format --verify-no-changes`.

## 0.4.0-alpha.1 – 2026-10-10

129 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### Fixed: two problems in 0.3.0-alpha.1

- **Builds broke after `init`:** the SDK's IDE0065, which `init` turned on, could move usings out of a
  namespace and change what names refer to (CS0104/CS0118). It is replaced by **BRO1008**, which only
  moves usings when every name still means the same, and does nothing unless `csharp_using_directive_placement`
  is set (the preset sets `outside_namespace`).
- **Renames changed behavior through `nameof`:** a name that `nameof(...)` turns into a string is no longer
  renamed.

### New

- **SonarQube support:**
  - `stylebro-migrate` reads a team's Sonar setup (`.editorconfig`/`.globalconfig`, rulesets, or
    `--sonar-profile <exported profile>`) and turns on the StyleBro and SDK rules that fix what those Sonar
    rules report. Sonar suppressions are carried over.
  - Three new rules mirroring Sonar's: **BRO1149** merge nested ifs (S1066), **BRO1150**
    `Where(p).Count()` → `Count(p)` (S2971), **BRO1151** no explicit array for `params` (S3878).
  - With Sonar's package in the project, `dotnet format` also fixes Sonar's own S1066, S2971 and S3878.
- **`init` adapts to the repo:** it detects the repo's conventions for StyleBro's settings (`=>` and operator
  placement, trailing commas, `""`, null checks, using placement, `_field` names, ...) and keeps the
  majority style.

### Changed

- **Public API isn't renamed by default:** naming rules skip names visible outside the assembly;
  `stylebro_rename_public_api = true` to include them.
- **`stylebro-migrate format`** repeats until a run changes nothing (at most 3) and says how many files each
  run changed.

### Fixed

- **Fix order:**
  - Fixes that needed a second `dotnet format` run now converge in one: comments below a field, blank lines
    around documented properties, `: base(...)`/`where` at the line limit.
  - BRO1603 no longer adds a period after custom doc elements.
- **Migration and `init`:**
  - EF Core migrations are treated as generated code.
  - One-line `delegate { … }` statements are no longer split.
  - The tool asks which solution to use when there are several.
  - `--diff` samples show the right change.

### Upgrading from 0.3.0-alpha.1

Bump the package, then update the tool:
`dotnet tool update -g StyleBro.Migrate --prerelease`.
Run `stylebro-migrate init --diff` (or `<path> --diff` if you migrated from StyleCop) to preview the changes.

## 0.3.0-alpha.1 – 2026-10-09

125 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### New

- **BRO1410:** unnecessary parentheses in patterns (`x is (> 0)` → `x is > 0`). Unnecessary
  parentheses (BRO1405, BRO1410) are now also faded in the IDE, like StyleCop's SA1119_p.
- **11 settings where teams prefer otherwise:**
  - `default` literal;
  - `""` instead of `string.Empty`;
  - omit trailing commas;
  - empty `{ }` on one line;
  - `)` on its own line;
  - first item on the `(` line;
  - one-line members together;
  - `<inheritdoc />`;
  - `: base(...)` / `where` on the declaration line;
  - `l`-only literal suffixes.

  See [Settings](https://bisforboman.github.io/stylebro/configuration/).
- **`stylebro-migrate --diff`:** previews `init`, a migration or `format` on a temporary copy.
  Prints changes per rule and writes the full diff to a `.patch` file.
- **Docs site with search:** each rule's help link now opens its page there.

### Changed

- **`stylebro-migrate format`** applies only StyleBro's and the SDK style rules' fixes;
  `--all` for everything.
- **`init` adapts to the repo:** it keeps `_field` names when they're the majority, and points
  to `--write` when it finds a StyleCop setup.
- **BRO1604** only rewrites summaries that start like a noun phrase.

### Fixed

- **Migrating:** StyleCop settings kept in a git submodule are read, and `format` no longer
  edits submodule files.
- **File header:** never set up with StyleCop's placeholder company.
- **Region removal (BRO1517):** several empty regions in a row now go in one run.
- **Fix order:** several cases that needed a second `dotnet format` run now converge in one
  (member sorting with blank lines, call chains with `#region`, documented fields).
- **Uneditable files:** no findings in NuGet `contentFiles`.

### Upgrading from 0.2.0-alpha.1

Bump the package and the tool:
`dotnet tool update -g StyleBro.Migrate --prerelease`.
Run `stylebro-migrate init --diff` (or `<path> --diff` if you migrated from StyleCop) to see what
the new version would change before running it for real.

## 0.2.0-alpha.1 – 2026-10-06

53 new rules, `stylebro-migrate format` for multi-targeted repositories, the PR pipeline (parity, real-world runs, mutation testing).

## 0.1.0-alpha.8 – 2026-10-02

Baseline support and a BRO1117 crash fix; 71 rules.

## 0.1.0-alpha.7 – 2026-10-02

71 rules.

## 0.1.0-alpha.6 – 2026-10-01

54 rules; the first prerelease published without an approval step.

## 0.1.0-alpha.5 – 2026-10-01

The `stylebro-migrate` tool (StyleBro.Migrate).

## 0.1.0-alpha.4 – 2026-10-01

43 rules.

## 0.1.0-alpha.3 – 2026-09-30

18 rules.

## 0.1.0-alpha.2 – 2026-09-30

11 rules, rule pages with tested examples, Fix All in multi-targeted projects.

## 0.1.0-alpha.1 – 2026-09-30

The first release: BRO1001 member ordering.
