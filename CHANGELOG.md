# Changelog

Versions are git tags (`vX.Y.Z`, prereleases `vX.Y.Z-alpha.N`); each one publishes StyleBro.Analyzers and
StyleBro.Migrate to nuget.org and a GitHub release with its section below as the notes. What a version number may
change: [docs/versioning.md](docs/versioning.md). What's merged but not released yet is in [changes/](changes/) (one
file per pull request).

## 0.6.0-alpha.1 – 2026-10-12

129 rules, each fixed by `dotnet format`. Docs: https://bisforboman.github.io/stylebro/

### Breaking changes

- **The preset no longer sorts usings.** It stopped setting `dotnet_sort_system_directives_first` (`dotnet format`
  sorts usings whenever that key is set, to any value). `stylebro-migrate init` writes the key when your usings are
  already sorted (`true`, or `false` when System doesn't come first); for unsorted or mixed code it writes a comment on
  how to turn sorting on.
- **Sonar mappings never fix what the Sonar rule accepts** (`stylebro-migrate`):
  - S100, S101, S1118, S1125, S1481, S1854, S1905, S2325, S3052, S3260 and S3442 are no longer mapped: their StyleBro
    or .NET rules report code those Sonar rules accept (IDE0004 removed `(int)` casts S1905 allows; CA1852 sealed
    classes with `protected` constructors). The report lists them under "Not covered" with the reason.
  - S121, S818, S1066, S1116, S3878 and S8969 turn on their rule with a new setting that makes it report exactly what
    Sonar reports (see New). Where the StyleCop setup turns the same rule on (SA1106, SA1503), StyleCop's wider scope
    stays and the report says so.
  - S927 turns BRO1313 on with `stylebro_match_base_parameter_names = non_generic_methods`, which skips what S927 skips.
    S4136 maps to `stylebro_keep_overloads_together = same_access`.
- **A StyleCop migration applies only where StyleCop ran:** `stylebro-migrate --write` finds the projects that
  reference StyleCop.Analyzers (directly or through Directory.Build.props/.targets or Directory.Packages.props) and
  writes settings and turns the preset off for their folders only. Other folders are listed in the report (`init` can
  set them up later) and `format` leaves them alone. Each project's own ruleset and `stylecop.json` are read; projects
  that differ from the rest get their own `.editorconfig` section.
- **More field renames go through:** BRO1303/BRO1306 keep a field's name only for a string where code reaches a member
  by name (`GetField("_count")`, `GetProperty(...)`, `Type.GetType(...)`, `[MemberData("...")]`, an attribute's
  `Name = "..."`). Before, the name as any string anywhere in the repository (an exception's parameter name, a JSON key)
  kept the rename, so most renames in large repositories were left as warnings.
- **`stylebro-migrate format --verify-no-changes` fails (exit code 1) when a project wasn't loaded in any run**, instead
  of reporting "Clean." for code it never checked. Projects skipped for some target frameworks only, or because they
  reference an F# project, stay warnings.
- **Old-style (non-SDK) projects:** `format` no longer runs `dotnet restore` on them (it broke the next Visual Studio
  build); restore them with `msbuild -t:restore -p:RestorePackagesConfig=true` first. In `packages.config` projects
  StyleBro.Analyzers is installed the packages.config way (the entry, the `<Analyzer>` items and the build targets
  import), and a `PackageReference` in `Directory.Build.props` gets a condition that leaves them out: it switched them
  to PackageReference restore, so a fresh clone didn't build. The old-style section of Getting started shows the lines.

### New

- **`stylebro-migrate` for scripts and AI agents** ([Using StyleBro from scripts and AI
  agents](https://bisforboman.github.io/stylebro/agents/)):
  - `--json` on every command: one JSON object on stdout (settings written, conventions found, files changed per run,
    kept findings with their reason, exit code), the human text on stderr.
  - `format --files a.cs b.cs` formats only those files and loads only their projects: seconds instead of minutes on
    a large solution. Works with `--verify-no-changes`.
  - `--agents-md` on `init --write` and `--write` writes a short StyleBro section into `AGENTS.md` (replaced on a
    re-run, the rest of the file kept).
  - `llms.txt` and `llms-full.txt` on the docs site: every page and rule in one index.
- **Settings that make six rules report exactly what their Sonar rule reports:**
  - BRO1514 `stylebro_braces_statements = if_and_loops` (S121): `using`, `lock`, `fixed` and deconstructing `foreach`
    bodies need no braces; applies to BRO1515 and BRO1516 too.
  - BRO1135 `stylebro_upper_case_literal_suffixes = lone_l` (S818): only a suffix that is just `l`.
  - BRO1149 `stylebro_allow_nested_dynamic_ifs` (S1066): nested `if`s with a `dynamic` condition aren't merged.
  - BRO1101 `stylebro_allow_semicolon_after_closing_brace` (S1116): the `;` after a type's or namespace's `}` stays.
  - BRO1151 `stylebro_params_arrays = except_object_and_jagged` (S3878): arrays for `params object[]`, `params Array[]`
    and jagged `params` arrays stay.
  - BRO1147 `stylebro_allow_null_forgiving_on_unannotated` (S8969): a `!` on a value declared without nullable
    annotations stays.
- **More settings:**
  - BRO1313 `stylebro_match_base_parameter_names = all | non_generic_methods`.
  - `stylebro_private_static_field_naming`: the casing of private constants and `static readonly` fields (PascalCase,
    camelCase or `_camelCase`).
  - `stylebro_private_mutable_static_field_naming = camelCase | _camelCase`: private static fields that aren't readonly
    or const, when their style differs from the other private fields' (StyleCop's SX1309S).
  - `stylebro_allow_leading_underscore_in_pascal_fields` (BRO1306): `true` leaves constants, `static readonly` and
    non-private fields that start with `_` alone, like StyleCop with SA1309 off; `stylebro-migrate --write` sets it
    from SA1309.
  - `stylebro_keep_overloads_together = same_access`: overloads with the same accessibility, `static` and `abstract`
    stay together (Sonar's S4136).
- **Finding the solution:** `format`, `baseline` and `--diff` run on a repository whose root build is a Traversal
  project (`Sdk="Microsoft.Build.Traversal"`), and with no solution or project at the root they use the only solution
  one folder down (`src/MySolution.slnx`) and say so. A folder with one solution and several filters of it (`.slnf`)
  uses the solution.
- `stylebro-migrate --version`.

### Changed

- **`stylebro-migrate init` follows more of your code's conventions.** Where the code is clear, `init` writes its style;
  where it's mixed, it turns the rule off instead of rewriting the code:
  - line endings, final newline and charset: `end_of_line`, `insert_final_newline` and `charset` follow the C# files
    (or `unset` when they're mixed, or when git converts the files on checkout), also when your `.editorconfig` says
    otherwise; the report says how to switch to the key's value instead. `stylebro-migrate --write` does the same;
  - naming: the casing of private constants and `static readonly` fields (BRO1306), `s_`/`t_` prefixes (BRO1307),
    `String`/`Int32` instead of `string`/`int` (IDE0049);
  - members left `private` by default (`dotnet_style_require_accessibility_modifiers = omit_if_default`, or BRO1404
    off), member order (BRO1001), overloads kept together, `<inheritdoc/>` on overrides (BRO1601);
  - one-line `if`/loop/`case` statements (`csharp_preserve_single_line_statements`), one-line jumps
    (`stylebro_allow_single_line_jump_statements`), auto-properties over several lines (BRO1527), list items grouped on
    lines (BRO1108), `#region`s inside members (BRO1113), combined declarations (BRO1142 for locals, BRO1114 for
    fields);
  - usings written relative to their namespace (BRO1126), lower-case unsigned suffixes `0u` (BRO1135), property and
    constructor summaries in their own words (BRO1604, BRO1606), file header templates with empty `//` lines (BRO1120);
  - other formatters: with CSharpier (`.csharpierrc*`, the `csharpier` tool or `CSharpier.MsBuild`), IDE0055 and the
    32 layout rules CSharpier decides are off (the list is in Getting started); with ReSharper/Rider column alignment
    (`resharper_int_align_*`), IDE0055 is off.
- **`init` and your `.editorconfig`:**
  - when most of the code contradicts a key the repository sets (operator placement, `csharp_prefer_braces = true`,
    using placement, `always_for_clarity` parentheses), or the code is mixed under a key nothing enforces, `init` keeps
    the key and turns off the StyleBro rule, with a comment saying why; a key set at severity `silent` or `none` isn't
    enforced, so `init` follows the code instead;
  - a folder with its own `root = true` `.editorconfig` gets a block of its own, judged from its own code (also with
    `stylebro-migrate --write`); folders marked `generated_code = true` don't count;
  - running `init` again keeps the convention lines and the turned-off rules the last run wrote; delete a line to have
    it counted again;
  - it says when `dotnet_sort_system_directives_first` will make `format` reorder usings, that
    `trim_trailing_whitespace = false` isn't read by `dotnet format`, and which CI steps (GitHub workflows, Azure
    Pipelines) still run plain `dotnet format --verify-no-changes`.
- **Rules:**
  - **BRO1514-BRO1516** leave `fixed (a) fixed (b) { }` alone, like consecutive `using`s;
    `stylebro_allow_consecutive_usings = false` nests them again.
  - **BRO1523** leaves a chain alone when its first line already holds two or more calls.
  - **BRO1606** reports only empty summaries, filler (`Default constructor.`, replaced by the standard sentence; text
    after it stays) and the standard sentence written differently, no longer a summary in its own words.
  - **BRO1604** no longer reports a `bool` property's summary that starts with an article, and asks for "a value
    indicating whether" on `bool` only, not `bool?`.
  - **BRO1504** needs no blank line above a `//` comment between the items of a list or inside a call chain.
  - **BRO1401** no longer checks property patterns, like StyleCop 1.2.
  - **BRO1611, BRO1613, BRO1614** skip members documented with `<inheritdoc>`, like StyleCop.
  - **Naming rules** (BRO1303, BRO1306-BRO1308, BRO1310) leave interop names alone: `*NativeMethods` classes,
    SCREAMING_CASE constants and `static readonly` fields (`S_OK`), COM GUID fields, and fields of types with
    `[StructLayout]`, `[ComImport]`, `[UnmanagedFunctionPointer]` or `[DllImport]`/`[LibraryImport]` methods.
    BRO1303/BRO1306 leave private static fields alone when a `dotnet_naming_rule` asks for a style StyleBro doesn't
    write (`s_`), and BRO1306 renames the public fields of a private nested class.
  - **BRO1142** follows `csharp_style_var_*`; **BRO1108** with BRO1107 off keeps the first item on the `(` line;
    **BRO1149** puts a merged condition on its own line when the outer one is wrapped; **BRO1122** writes `6u`/`1ul`
    when BRO1135 is off.
  - Nothing is reported in files a source generator writes, whatever their name or header.
- **Faster:** syntax-only analyzers no longer make the compiler bind every method body and Fix All runs in parallel
  (a full `dotnet format` of Newtonsoft.Json takes about half the time); `stylebro-migrate`'s first run evaluates
  projects once and in parallel (35-60 % less time for `format` and `init --diff`). Same results.
- When the settings turn IDE0055 off, `format` skips the whitespace pass.

### Fixed

- **Fixes that needed a second `dotnet format` run** now converge in one: BRO1101 on `};` before a blank line or
  comment, BRO1122 with BRO1405 (`((double)0).ToString()` → `0D.ToString()`), BRO1509's expanded properties (BRO1505's
  blank line), BRO1514-BRO1516 on an `if`/`else` chain, BRO1126 when `dotnet format` sorts usings, BRO1149 next to
  `#region`, member sorting with region removal, BRO1107 with BRO1108. BRO1527 and IDE0055 no longer change a
  declaration whose type spans lines back and forth.
- **Rules:**
  - BRO1001 keeps the order of a type whose members are read by position through reflection (`GetFields()[0]`).
  - BRO1301/BRO1302 no longer rename query range variables and lambda parameters that end up in an expression tree.
  - BRO1303/BRO1306 no longer rename a field to its class's name; a `nameof` keeps a name only when it refers to that
    symbol; renames in multi-targeted repositories see every project's code (a test's `GetField("count")`).
  - BRO1602 fixes a `///` comment after code on the same line; BRO1120 leaves the header's empty `//` lines alone when
    `file_header_template` is set; BRO1125 writes `() => { }` on one line for a one-line anonymous method.
  - BRO1603 no longer adds a period after text that ends in code (`x = 1`); BRO1107/BRO1108 skip lists with a
    multi-line string; `stylebro_blank_line_between_switch_sections = omit` is no longer overruled; BRO1149 puts `&&`
    at the line end when BRO1520 is off.
  - File headers with `;` or `#` (licence notices) were cut off: BRO1615's settings are escaped now.
- **`stylebro-migrate format`:**
  - gives the same result on every OS: a file keeps its line ending, and `--verify-no-changes` ignores findings that
    only swap it;
  - no longer reports "clean" when StyleBro's analyzers didn't load: it stops before `dotnet format` and names projects
    without them;
  - restore: skips NuGet's audit (NU1901-NU1904 with warnings as errors), explains NU1102 (package source mapping),
    names the NuGet error; `--no-restore` works for multi-targeted repositories;
  - projects: two projects compiling the same files run apart instead of crashing (a crash names the run's projects
    and the files already changed); test projects referencing a single-target project load; linked files
    (`<Compile Include="..\Shared\*.cs" />`) are formatted and found by `--files`; `.slnf` filters get the same
    per-framework runs as their solution; an SDK-style project with a leftover `packages.config` is SDK-style;
    `net6`/`net60`/`net6.0` share one run; a hand-written `Migrations` folder is formatted;
  - a solution in a subfolder respects the migration's "No StyleCop ran in" folders;
  - clear errors for a project MSBuild can't evaluate, a malformed `.slnf`, a solution without C# projects, files with
    old Mac line endings and lone surrogates in strings; `--once`/`--project` aren't passed on to `dotnet format`;
  - output: a file counts as changed only when its content changed; kept findings say where the string is and are
    listed (marked `kept`) when a run ends "Not clean"; `--verify-no-changes` shows `dotnet format`'s real error.
- **Migration and `init`:**
  - Sonar severities per folder (sub-directory `.editorconfig` and path sections) and `.editorconfig` sections with
    brace lists (`[{src/**/*.cs,tests/**/*.cs}]`) are followed.
  - Sonar rules follow the referenced SonarAnalyzer.CSharp version, and package versions in MSBuild properties
    (`$(StyleCopVersion)`) are read; suppressions only get the ids replacing Sonar rules that are on.
  - `--write` with SA1309 off no longer renames `_x` static readonly fields StyleCop accepted; StyleCop rules off by
    default (SA1305, ...) aren't turned on by a category-wide severity; StyleCop 1.0.x is read from `packages.config`
    and hint paths; with only SA1519/SA1520 on it writes `csharp_prefer_braces = true`.
  - A second `--write` after replacing StyleCop with StyleBro writes the same settings (the StyleCop version is
    recorded in the block); a second `init` keeps a rule it turned off.
  - Line endings: `end_of_line` is no longer set from a few files when git converts most; the files the tool edits keep
    their line endings.
  - `init` no longer writes a root `.editorconfig` when all code is under `root = true` folders, no longer calls the
    file header IDE0073's when `file_header_template = unset`, and on an already migrated repository says the settings
    are there. Frameworks set through a property count as multi-targeting.
  - `baseline` takes a solution or project first, like `format`.
- **`--diff` preview:** stops with git's error and handles long Windows paths; counts only real suppressions and shows
  each sample once; central package management (nearest `Directory.Packages.props`, `GlobalPackageReference`,
  `Remove`, a project's own CPM, projects that turn it off) and nested `Directory.Build.props` (NU1504) are handled;
  keeps byte order marks; goes on when a project doesn't load; refuses only `packages.config` projects up front (other
  old-style projects are previewed); no "StyleBro reported nothing" warning on clean code.

### Upgrading from 0.5.0-alpha.1

Bump the package, then update the tool: `dotnet tool update -g StyleBro.Migrate --prerelease`.

Then run `stylebro-migrate init --write` again (or `stylebro-migrate --write` if you migrated from StyleCop) to pick
up the new conventions; it keeps what it wrote before. Member order and suffixes are counted on the code as it is
now: after a `format` run that already sorted it, BRO1001 stays on.

- Relied on the preset to sort usings? Re-running `init` writes the key when your usings are sorted, or set
  `dotnet_sort_system_directives_first = true` yourself.
- Migrated from StyleCop with SA1309 off? `--write` adds `stylebro_allow_leading_underscore_in_pascal_fields = true`,
  so BRO1306 stops asking to rename `_x` static readonly fields.
- Old-style projects: restore with `msbuild -t:restore -p:RestorePackagesConfig=true` before `stylebro-migrate format`.

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
