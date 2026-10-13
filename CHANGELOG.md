# Changelog

Versions are git tags (`vX.Y.Z`, prereleases `vX.Y.Z-alpha.N`); each one publishes StyleBro.Analyzers and
StyleBro.Migrate to nuget.org and a GitHub release with its section below as the notes. What a version number may
change: [docs/versioning.md](docs/versioning.md). What's merged but not released yet is in [changes/](changes/) (one
file per pull request).

## 0.7.0-alpha.1 – 2026-10-13

129 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### New

- **Preset versions.** `<StyleBroPreset>` takes a version (`1.0`, later `1.1`, ...), `latest` or `none`, like .NET's
  `AnalysisLevel`: from 1.1 on, a release's new rules stay off until you raise it, so upgrading the package never adds
  warnings by itself. Unset means the package's major version (`1.0`); before 1.0 every rule is a 1.0 rule, so nothing
  changes yet. A version newer than the package warns (STYLEBRO002); any other value fails the build (STYLEBRO001).
  See https://bisforboman.github.io/stylebro/versioning/#new-rules

### Changed

- **Field renames follow your naming rules for non-private fields too** (BRO1303/BRO1306): the first
  `dotnet_naming_rule` that covers a public, internal or protected field, in Roslyn's order, decides its casing, but
  only when something enforces it (IDE1006's severity, or the rule's own, is `suggestion`, `warning` or `error`).
  A rule for camelCase internal fields no longer gets them renamed to PascalCase; a style StyleBro doesn't write (a
  prefix) leaves the name alone; an unenforced rule leaves StyleCop's defaults. `init` lets your code decide the
  private field style while IDE1006 is off.
- **StyleBro and CSharpier settle:** after `stylebro-migrate format` and `dotnet csharpier format .`, another `format`
  changes nothing.
  - `init` writes `stylebro_layout_by = csharpier` and `stylebro_layout_print_width` (the `.csharpierrc`'s
    `printWidth`, else `max_line_length`, else 100), and the opposite of a `dotnet_sort_system_directives_first =
    false` or `dotnet_separate_import_directive_groups = true` it finds, since CSharpier sorts usings System first.
  - BRO1149 lays out a merged condition the way CSharpier does (also `||` chains that don't fit); BRO1514-BRO1516
    count a statement CSharpier will split as multi-line, so its braces come in the same run.
  - `stylebro-migrate --write` (from StyleCop) detects CSharpier like `init` (IDE0055 and the same layout rules off;
    the report lists the StyleCop rules they replaced under "CSharpier decides it").
- **`init` follows more conventions:** spacing IDE0055 enforces (`if(x)`, `( a + b )`, `(int) x`, `F( x )`: a clear
  form is written as `csharp_space_*` keys, mixed code turns IDE0055 off), and blank lines between members (BRO1505)
  and above doc comments (BRO1513): compact or mixed code turns the rule off. `init` also takes `--project`.
- **Leftover StyleCop settings** (no project references StyleCop.Analyzers any more): `init` proceeds and follows the
  code, noting the lines; `--write` says StyleCop isn't installed and points to `init`.
- The preset file moved from `build/stylebro.recommended.globalconfig` to `build/presets/stylebro.1.0.globalconfig` in
  the package. Only matters if your build references the file by path.

### Fixed

- **Wrong code:** BRO1151 no longer unwraps a one-element array the call would take as the array itself
  (`Take(new Item[] { default! })` became `Take(default!)`, a null array; the compiler now decides), nor an array whose
  elements take their type from it (`M([new() { ... }])` didn't compile).
- **Rules:** BRO1123 and BRO1115 leave doc comment crefs alone (`Func{Nullable{int}}`), where the build warned and
  `dotnet format` had nothing to fix; BRO1311 keeps a tuple element's rename only for a use as a member in code the run
  doesn't load (`.value`, `value:`; internal members only where `InternalsVisibleTo` reaches), and says so.
- **Line endings, charset, final newline:** `init` judges them against the `.editorconfig` that decides each file (the
  nearest one setting the key) and writes its line there (a `# BEGIN stylebro-whitespace` block), so a nested
  `insert_final_newline = false` no longer makes `format` strip line breaks the report said it would keep.
- **In-solution source generators** (Mapperly): `format --verify-no-changes` and `--diff` previews run the generator in
  their copy too, so verify no longer fails where `format` is clean and previews no longer show renames `format` keeps
  back. Verify also stays clean where the restore installs git hooks (Husky.Net).
- **Migration where StyleCop is referenced conditionally:** the MSBuild `Condition` is evaluated, so settings go only
  where StyleCop ran (a `root = true` folder where it didn't gets none), and StyleBro.Analyzers is added next to the
  StyleCop reference under the same condition.
- **stylebro-migrate:** `baseline` skips the whitespace pass where IDE0055 is off (CSharpier), like `format`; `format`
  leaves projects no solution configuration builds alone and says so; a second `init` writes its lines in the same
  order; suppressions are no longer carried into generated code or onto IDE0055; lower-case `sa1234` ids are read; the
  line-ending and private-field (SA1309 in a sub-folder) notes judge the code correctly; `net4.7` and `net47` share
  one format run; clearer messages.

### Upgrading from 0.6.0-alpha.1

Bump the package, then update the tool: `dotnet tool update -g StyleBro.Migrate --prerelease`.

- `StyleBroPreset` unset means `1.0`, which is every current rule, so nothing changes. `stylebro-migrate init --write`
  pins `<StyleBroPreset>1.0</StyleBroPreset>` in `Directory.Build.props`; repositories migrated from StyleCop keep
  `none`.
- Run `stylebro-migrate init --write` (or `--write` if you migrated from StyleCop) again if you use CSharpier (for
  `stylebro_layout_by` and `stylebro_layout_print_width`), have a nested `.editorconfig` that sets line endings, the
  charset or the final line break, or space your code differently from the SDK's defaults.
- Have `dotnet_naming_rule` entries for non-private fields? Check the first `format` run: BRO1303/BRO1306 now follow
  the enforced ones.

Full list of changes: https://github.com/bisforboman/stylebro/compare/v0.6.0-alpha.1...v0.7.0-alpha.1

## 0.6.0-alpha.1 – 2026-10-12

129 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### Breaking changes

- **The preset no longer sorts usings** (it stopped setting `dotnet_sort_system_directives_first`, which makes
  `dotnet format` sort whenever it's set). `init` writes the key when your usings are already sorted, else a comment
  on how to turn sorting on.
- **Sonar mappings never fix what the Sonar rule accepts** (`stylebro-migrate`): S100, S101, S1118, S1125, S1481,
  S1854, S1905, S2325, S3052, S3260 and S3442 are no longer mapped (the report lists them with the reason); S121, S818,
  S1066, S1116, S3878 and S8969 use the new Sonar-matching settings below; S927 turns BRO1313 on with
  `stylebro_match_base_parameter_names = non_generic_methods`; S4136 maps to `stylebro_keep_overloads_together =
  same_access`. Where StyleCop turns the same rule on (SA1106, SA1503), StyleCop's wider scope stays.
- **A StyleCop migration applies only where StyleCop ran:** `stylebro-migrate --write` writes settings (and turns the
  preset off) only for the folders of projects that reference StyleCop.Analyzers, reading each project's own ruleset
  and `stylecop.json`. Other folders are listed in the report, and `format` leaves them alone.
- **More field renames go through:** BRO1303/BRO1306 keep a name only for a string that reaches a member by name
  (`GetField("_count")`, `Type.GetType(...)`, `[MemberData("...")]`, ...), no longer for any string with that text.
- **`stylebro-migrate format --verify-no-changes` exits 1 when a project wasn't loaded in any run**, instead of
  reporting "Clean." (projects skipped for some frameworks only, or for an F# reference, stay warnings).
- **Old-style (non-SDK) projects:** `format` no longer restores them; run `msbuild -t:restore
  -p:RestorePackagesConfig=true` first. `packages.config` projects get StyleBro.Analyzers the packages.config way, and
  the `Directory.Build.props` reference gets a condition that leaves them out (it broke their restore).

### New

- **`stylebro-migrate` for scripts and AI agents** ([docs](https://bisforboman.github.io/stylebro/agents/)): `--json`
  on every command, `format --files a.cs b.cs` (seconds instead of minutes), `--agents-md` for an `AGENTS.md` section,
  and `llms.txt`/`llms-full.txt` on the docs site.
- **Six rules can report exactly what their Sonar rule does:** `stylebro_braces_statements = if_and_loops` (BRO1514,
  S121), `stylebro_upper_case_literal_suffixes = lone_l` (BRO1135, S818), `stylebro_allow_nested_dynamic_ifs`
  (BRO1149, S1066), `stylebro_allow_semicolon_after_closing_brace` (BRO1101, S1116),
  `stylebro_params_arrays = except_object_and_jagged` (BRO1151, S3878), `stylebro_allow_null_forgiving_on_unannotated`
  (BRO1147, S8969).
- **More settings:** `stylebro_match_base_parameter_names = all | non_generic_methods` (BRO1313),
  `stylebro_private_static_field_naming`, `stylebro_private_mutable_static_field_naming`,
  `stylebro_allow_leading_underscore_in_pascal_fields` (BRO1306; `--write` sets it from SA1309),
  `stylebro_keep_overloads_together = same_access`.
- **Finding the solution:** Traversal projects, the only solution one folder down, and a solution next to several
  `.slnf` filters of it work without naming one. `stylebro-migrate --version`.

### Changed

- **`init` follows more of your code's conventions** (clear code sets the style, mixed code turns the rule off):
  line endings, charset and final newline (from the files, also against your `.editorconfig`); naming (static field
  casing, `s_`/`t_` prefixes, `String` vs `string`); layout (member order, one-line statements and jumps, multi-line
  auto-properties, grouped list items, `#region`s in members, combined declarations, implicit `private`, relative
  usings, summary wording); other formatters (CSharpier, ReSharper/Rider column alignment). A key the code contradicts,
  or one nothing enforces, turns off the rule instead; `root = true` folders get their own block; re-runs keep what the
  last run wrote.
- **Rules:** BRO1514-BRO1516 allow consecutive `fixed` statements (`stylebro_allow_consecutive_usings`); BRO1523 leaves
  chains whose first line has several calls; BRO1606 and BRO1604 leave summaries in your own words; BRO1504 needs no
  blank line before a comment in a list or call chain; BRO1401 skips property patterns; BRO1611/BRO1613/BRO1614 skip
  `<inheritdoc>`; naming rules leave interop names and `dotnet_naming_rule` prefixes alone; BRO1142 follows
  `csharp_style_var_*`; nothing is reported in source-generator output.
- **Faster:** a full `dotnet format` takes about half the time, and `stylebro-migrate`'s first run 35-60 % less.

### Fixed

- **One `dotnet format` run is enough** where several rules used to need a second one (BRO1101, BRO1122, BRO1126,
  BRO1149, BRO1509, BRO1514-BRO1516, BRO1527, member sorting with region removal, BRO1107 with BRO1108).
- **Rules that changed meaning:** BRO1001 keeps types read by position through reflection; BRO1301/BRO1302 leave
  expression-tree names; BRO1303/BRO1306 don't rename a field to its class's name; `nameof` and multi-targeted renames
  check the right symbols; file headers with `;` or `#` are kept whole.
- **`format` and `--diff` are sturdier:** the same result on every OS (each file keeps its line ending); no false
  "clean" when the analyzers didn't load; restores skip NuGet's audit and explain NU1102; projects sharing files,
  linked files, `.slnf` filters, central package management, nested `Directory.Build.props` and target framework
  spellings (`net6`, `net6.0`) are handled; clear errors instead of stack traces.
- **Migration and `init`:** Sonar severities per folder and per SonarAnalyzer version; versions from MSBuild properties;
  StyleCop 1.0.x; a second `--write` or `init` gives the same settings; edited files keep their line endings.

### Upgrading from 0.5.0-alpha.1

Bump the package, then update the tool: `dotnet tool update -g StyleBro.Migrate --prerelease`.

Run `stylebro-migrate init --write` again (or `stylebro-migrate --write` if you migrated from StyleCop) to pick up the
new conventions; it keeps what it wrote before. Code a `format` run already sorted keeps BRO1001 on.

- Relied on the preset to sort usings? Re-run `init`, or set `dotnet_sort_system_directives_first = true`.
- Migrated from StyleCop with SA1309 off? `--write` adds `stylebro_allow_leading_underscore_in_pascal_fields = true`,
  so BRO1306 stops renaming `_x` static readonly fields.
- Old-style projects: restore with `msbuild -t:restore -p:RestorePackagesConfig=true` before `stylebro-migrate format`.

Full list of changes: https://github.com/bisforboman/stylebro/compare/v0.5.0-alpha.1...v0.6.0-alpha.1

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
