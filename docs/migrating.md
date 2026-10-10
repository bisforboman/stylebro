# Migrating from StyleCop

Switching a StyleCop-clean repository to StyleBro shouldn't reformat code StyleCop was happy with. StyleBro's preset
follows StyleCop's defaults with a few deliberate exceptions ([differences-from-stylecop.md](differences-from-stylecop.md)),
but most teams have changed some of StyleCop's defaults anyway. `stylebro-migrate` reads what your team changed
and writes the matching settings.

```
dotnet tool install --global StyleBro.Migrate --prerelease
stylebro-migrate path/to/repo           # dry run: prints the report and the settings
stylebro-migrate path/to/repo --diff    # preview: what the settings and the format run would change in the code
stylebro-migrate path/to/repo --write   # writes them
```

`--diff` runs `--write` and then `stylebro-migrate format` (until a run changes nothing) on a temporary copy of the
repository, prints which files each rule changed with a few sample hunks, and writes the full diff to
`stylebro-preview.patch` (`--diff=file` for another name, `--keep` keeps the copy). The copy also gets the
StyleBro.Analyzers reference if the repository has none; StyleCop.Analyzers stays, but format only fixes StyleBro's
and the built-in rules' ids, so it changes nothing. In a StyleCop-clean repository the format part should be close to
empty. Details: [Preview first](getting-started.md#preview-first).

The tool is on nuget.org from 0.1.0-alpha.5, released with the analyzers under the same version (latest: 0.4.0-alpha.1;
[which version](getting-started.md#which-version) says how to build `main` instead). To run it straight from a clone: `dotnet run --project src/StyleBro.Migrate -- path/to/repo`.

Then swap the package (below) and run `stylebro-migrate format`. It runs `dotnet format` with only StyleBro's rules
and the built-in rules the block turns on (plus whitespace formatting), once per target framework where projects
target several, and never inside git submodules. Plain `dotnet format` also applies every other analyzer's fixes and
the compiler's: in one trial repository it added `required` (CS8618) and a Sonar fix removed `init;` accessors, nine
build errors. The plain equivalent is `dotnet format --diagnostics <ids>` with every id to fix (`--diagnostics BRO1001
BRO1505 IDE0055 IDE0036 ...`); `stylebro-migrate format --all` keeps everything.

## Swap the package

Remove StyleCop and add StyleBro wherever the repository references StyleCop. Look for both package ids,
`StyleCop.Analyzers` and the prerelease `StyleCop.Analyzers.Unstable`, in `.csproj`, `Directory.Build.props` and
`Directory.Packages.props`. With central package management the reference is often a `GlobalPackageReference`, which
applies to every project:

```xml
<!-- Directory.Packages.props, before -->
<GlobalPackageReference Include="StyleCop.Analyzers.Unstable" Version="1.2.0.556" />

<!-- after -->
<GlobalPackageReference Include="StyleBro.Analyzers" Version="0.4.0-alpha.1" />
```

`stylecop.json` and the StyleCop suppressions can stay; StyleBro ignores them. The steps in order:
[getting-started.md](getting-started.md#b-coming-from-stylecop). A small StyleCop project migrated step by step, with
real output: [samples/StyleCopMigration](https://github.com/bisforboman/stylebro/blob/main/samples/StyleCopMigration/README.md).

## What it reads

- StyleCop's own defaults (from the StyleCop 1.2 DLL's rule list).
- `*.ruleset` files, then `*.globalconfig` files, then the root `.editorconfig`'s sections for all C# files. Later
  sources win, like in the compiler: `dotnet_diagnostic.SAxxxx.severity` and
  `dotnet_analyzer_diagnostic.category-StyleCop.CSharp.*.severity` keys, and ruleset `<Rule Id="SAxxxx">` actions.
  Also like in the compiler, a bulk setting (a category, or `dotnet_analyzer_diagnostic.severity` for every rule)
  doesn't override a rule's own entry in a ruleset or another config. A ruleset's `<Include>`d rulesets count, under
  its own entries. When several rulesets or global configs disagree (one per project type), the strictest wins, a file
  that doesn't mention a rule counting with StyleCop's default, so production code keeps everything StyleCop enforced
  there.
- A ruleset that a folder's `Directory.Build.props` selects (`<CodeAnalysisRuleSet>`, itself or through what it
  imports) applies to that folder: a `tests` folder whose ruleset turns ordering off gets BRO1001 off in
  `tests/.editorconfig`, while the rest keeps it.
- Settings the repository's MSBuild files point to are read wherever they are, also inside a git submodule (a shared
  infrastructure repository): imported `.props`/`.targets` (for `GenerateDocumentationFile` and the StyleCop version),
  the rulesets they select, and a `stylecop.json` added as `<AdditionalFiles>`. They're only read.
- Sub-directory `.editorconfig` files and path-specific sections (like `[tests/**.cs]`) are translated in place: they
  get the settings that differ from the repository-wide ones, in the same file and section. A folder that turns every
  analyzer off (`dotnet_analyzer_diagnostic.severity = none`, common for vendored code) gets every replacing rule as
  `none`: a rule the root turns on by its id would win over the folder's bulk setting.
- Git submodules and other nested repositories (folders with their own `.git` file or folder) are otherwise skipped:
  their settings, code and project files belong to another repository, and nothing in them is changed
  (`stylebro-migrate format` excludes them too). Files a project
  links from a submodule are another matter: see [excluding vendored code](getting-started.md#excluding-vendored-code).
- The StyleCop.Analyzers version in the project files: with 1.1.x, the rules added in 1.2 (SA1141, SA1142, SA1316,
  SA1414) count as off.
- Whether any project sets `GenerateDocumentationFile`. Without it the build doesn't parse XML documentation and
  StyleCop's rules that read it never run (it reports SA0001 instead), but `dotnet format` does parse it. So the
  StyleBro rules that read documentation as XML (BRO1603-BRO1611) stay off.
- StyleCop's alternative rules: SX1101 (no `this.`) turns on the SDK's IDE0003, SX1309 (fields begin with `_`) sets
  BRO1303 to `_camelCase`, which also covers SX1309S (private static fields begin with `_`; on its own, with instance
  fields named `count`, the report lists it as not expressible: BRO1303 has one style for both), and SA1412 (UTF-8 with BOM) writes `charset = utf-8-bom`.
- `stylecop.json`: `elementOrder`, `usingDirectivesPlacement`, `systemUsingDirectivesFirst`,
  `blankLinesBetweenUsingGroups`, `allowBuiltInTypeAliases`, indentation, the file header settings,
  `documentationCulture` and `excludeFromPunctuationCheck`.

## What it writes

A block between `# BEGIN stylebro-migrate` and `# END stylebro-migrate` in each `.editorconfig` it changes. Running the
tool again replaces the block, so put your own settings outside it.

- **StyleBro rules.** A rule is on only when StyleCop enforced every rule it replaces, with the weakest of their
  severities. [BRO1001](rules/BRO1001.md) replaces SA1201-SA1204 and SA1214 with one sort. A team that turned SA1201
  off doesn't get the full sort, so BRO1001 stays off and the report lists SA1203/SA1204 as partly covered. Rules
  that `stylecop.json` makes moot don't count: SA1203 when `elementOrder` leaves out `constant`, and so on. When
  `elementOrder` leaves out `kind` or `accessibility`, BRO1001 stays off: it always sorts by both.
- **Private field naming.** [BRO1303](rules/BRO1303.md) uses `_camelCase` when SA1309 (no leading underscore) is off
  and most of the repository's private fields start with `_`, and `camelCase` otherwise. Private constants and
  `static readonly` fields are pinned to StyleCop's PascalCase (`stylebro_private_static_field_naming = PascalCase`,
  [BRO1306](rules/BRO1306.md)): without it BRO1306 would follow a camel-case `dotnet_naming_rule` for static fields that
  StyleCop never enforced (IDE1006 off), and rename fields StyleCop was happy with.
- **SDK rules.** The rules StyleBro relies on (IDE0055 formatting, IDE0036 modifier order, IDE0049 type aliases, and so
  on) get the strongest severity of the StyleCop rules they cover, with options from `stylecop.json`.
- **Every rule by its id.** Each StyleBro rule gets its own `dotnet_diagnostic.BROxxxx.severity` line, `none` included
  (also the rules beyond StyleCop and those off by default): a bulk `dotnet_analyzer_diagnostic.severity = warning`
  in your `.editorconfig` would otherwise turn them on.
- **File header.** With StyleCop's XML header and a `companyName`, [BRO1615](rules/BRO1615.md) is on and IDE0073 off;
  with `xmlHeader: false`, IDE0073 (with `file_header_template`) and BRO1615 off. Never both: each would add its
  header above the other's on every run. Without a `companyName` BRO1615 stays off (StyleCop's default company,
  `PlaceholderCompany`, is nobody's real header).
- **A build that copies `.editorconfig`.** Some shared build setups copy an `.editorconfig` over the root one on every
  build (SixLabors' shared infrastructure does). The report notes such a file: the block would be lost on the next
  build, so put it into the copied file (or stop the copy).

- **Documentation scope.** `documentExposedElements`, `documentInternalElements` and `documentPrivateElements`
  become [BRO1601](rules/BRO1601.md)'s `stylebro_document_*` settings.
- **Documentation language and punctuation.** A `documentationCulture` other than English (`de-DE`, `fr-FR`; any
  `en-*` counts as English) turns off [BRO1604](rules/BRO1604.md)-[BRO1607](rules/BRO1607.md), which write English
  summary sentences (StyleCop checks translated ones); the report says so. `excludeFromPunctuationCheck`, when it
  differs from StyleCop's default (`["seealso"]`), becomes [BRO1603](rules/BRO1603.md)'s
  `stylebro_exclude_from_punctuation_check`.

Every key the preset sets is written, and `--write` turns the preset off (`<StyleBroPreset>none</StyleBroPreset>` in
every `Directory.Build.props` the projects import: the root one, created if needed, and a nested one that doesn't import
the root's): the block replaces it. That matters because an `.editorconfig`
can't take a key back from the preset: `dotnet format` sorts `using` directives whenever
`dotnet_sort_system_directives_first` is set, even to `false`, so the block sets it only when StyleCop sorted usings
(SA1208 or SA1210 on). SDK settings your root
`.editorconfig` already sets for C# files are left out: your code is already formatted with them.

## Suppressions

With `--write`, StyleCop suppressions in the code and in MSBuild files (`<NoWarn>` in `.csproj`, `.props`,
`.targets`) get the replacing rules too, so code your team deliberately exempted
stays exempt:

```csharp
#pragma warning disable SA1202, SA1642            // before
#pragma warning disable SA1202, SA1642, BRO1001, BRO1606   // after

[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "...")]
[SuppressMessage("Style", "IDE0009", Justification = "...")]    // added
```

```xml
<NoWarn>$(NoWarn);SA1123;SA1600;BRO1113;BRO1601</NoWarn>
```

The StyleCop suppressions stay: they're harmless once StyleCop is gone, and needed while both run. Running the tool
again adds nothing. Sonar's suppressions get the same treatment ([Coming from SonarQube](#coming-from-sonarqube)).

EF Core migrations are `dotnet ef`'s output: when the repository has them, the block marks their folders
`generated_code = true` (a section per folder, e.g. `[src/Ordering.Infrastructure/Migrations/**]`), so formatting
and StyleBro leave them alone.

## The report

The dry run lists the StyleCop rules that are on but that nothing enforces after the switch:

- **Partly covered or not expressible:** the StyleBro rule also enforces rules your team turned off (BRO1001 sorts by
  kind, accessibility and static together, so SA1203/SA1204 alone can't be carried over), or a setting can't be
  expressed for the SDK.
- **Dropped by design:** there's no safe automatic fix (the fix would be placeholder documentation, a file rename, or
  a changed public API).
- **Not covered yet:** candidates for future StyleBro rules. See [stylecop-mapping.md](stylecop-mapping.md).

## Coming from SonarQube

Many teams run SonarQube or SonarCloud next to (or instead of) StyleCop. `stylebro-migrate` and `stylebro-migrate init`
read that setup too and turn on the StyleBro and .NET rules that fix what the team's Sonar rules report, so
`stylebro-migrate format` cleans up findings Sonar can only list. Sonar stays: StyleBro doesn't turn any Sonar rule off,
and Sonar keeps reporting its own ids.

What it reads, in this order (later wins, like for StyleCop):

- **The base.** A quality profile exported from the server, when you pass one: `--sonar-profile profile.xml` (the XML from
  the server's `api/qualityprofiles/backup?language=cs&qualityProfile=<name>`, or Quality Profiles > Back up in the UI;
  its `csharpsquid` rules are on). Without it, when a project references `SonarAnalyzer.CSharp` (`PackageReference`,
  `GlobalPackageReference` or central package versions), the package's own defaults ("Sonar way", from SonarAnalyzer.CSharp
  10.35's rule list).
- **Rulesets** with Sonar rules (`<Rules AnalyzerId="SonarAnalyzer.CSharp">`): your own, SonarLint's
  `.sonarlint/*csharp.ruleset`, and the scanner's `.sonarqube/conf/Sonar-cs.ruleset` when it's there (rulesets named
  `*none*` are skipped).
- **Global configs** (`dotnet_diagnostic.Sxxxx.severity`); with several (one per project type) the strictest wins.
- **The root `.editorconfig`'s** sections for all C# files.

Without a Sonar package, a profile or Sonar rule ids in those files, nothing Sonar-related happens.

The Sonar rules that are on turn on these rules, at the strongest severity of the Sonar rules behind each:

| Sonar | Fixed by |
|---|---|
| S100, S101 (PascalCase names) | [BRO1309](rules/BRO1309.md) |
| S121 (braces) | [BRO1514](rules/BRO1514.md) with `csharp_prefer_braces = true` |
| S818 (upper-case literal suffixes) | [BRO1135](rules/BRO1135.md) with `stylebro_upper_case_literal_suffixes = l_only` |
| S927 (parameter names match the base) | [BRO1313](rules/BRO1313.md) |
| S1066 (mergeable `if`s) | BRO1149 |
| S1116 (empty statements) | [BRO1101](rules/BRO1101.md) |
| S1118 (utility classes) | CA1052, non-public classes only (`dotnet_code_quality.CA1052.api_surface = private, internal`) |
| S1125 (redundant boolean literals) | IDE0075, IDE0100 |
| S1481, S1854 (unused locals and assignments) | IDE0059 |
| S1659 (one variable per declaration) | [BRO1142](rules/BRO1142.md) |
| S1905 (redundant casts) | IDE0004 |
| S1939 (redundant base types) | [BRO1408](rules/BRO1408.md) |
| S2325 (members that could be static) | CA1822, non-public members only (`dotnet_code_quality.CA1822.api_surface = private, internal`) |
| S2971 (LINQ predicate into `Count`/`Any`/...) | BRO1150 |
| S3052 (initialized to the default value) | CA1805 |
| S3260 (seal private classes) | CA1852 |
| S3442 (public constructors of abstract classes) | CA1012 |
| S3878 (arrays for `params`) | BRO1151 |
| S4136 (overloads together) | [BRO1001](rules/BRO1001.md)'s `stylebro_keep_overloads_together = true`, only when BRO1001 is on |
| S8969 (redundant `!`) | [BRO1147](rules/BRO1147.md) |

**Sonar's own warnings for S1066, S2971 and S3878** are fixed too when the project references SonarAnalyzer.CSharp:
BRO1149-BRO1151's fixes are registered for those ids, so `dotnet format` (or a light bulb) applies them to Sonar's
diagnostics, at the places the StyleBro rule would fix; where it skips the code (a comment in the merged text, an array of a
derived type, Sonar's other S2971 cases, ...) the Sonar warning stays. This needs the package in the build: findings only
the scanner reports in CI aren't seen by `dotnet format`. Checked against SonarAnalyzer.CSharp 10.35.0.4138
(`scripts/sonar-interop.ps1`).

The data lives in the tool: `src/StyleBro.Migrate/data/sonar-mapping.tsv` (the mapping) and
`sonar-rules-10.35.0.4138.tsv` (Sonar's rule list and defaults, public metadata only).

**With StyleCop too**, a rule is on when either setup turns it on, at the stronger severity: the migration normally turns
the rules beyond StyleCop off (BRO1135, BRO1142, BRO1313, ...), but not when the matching Sonar rule is on. A setting
Sonar's rule needs replaces the one from the StyleCop setup, and the report lists each such conflict (S121's
`csharp_prefer_braces = true` where StyleCop's braces rules were off). An option of a rule StyleCop's setup leaves off
isn't applied: S4136 only sets an option of BRO1001, so with SA1201 or SA1202 off it stays Sonar's alone.

The report has a Sonar part: what was read, how many Sonar rules are on, which of them StyleBro or the SDK now fixes (and
with which rule), which mapped ones aren't applied and why, and that the rest stay Sonar's (no safe automatic fix: unused
private members, cognitive complexity, ...). The generated block has the Sonar-driven lines last, under
`# From the SonarQube setup`, each with the Sonar rule's title. `--diff` prints the same part of the report before its
summary. `init` does the same in a repository without StyleCop, and prints it.

Suppressions of Sonar ids are carried over like StyleCop's ([Suppressions](#suppressions)): `#pragma warning disable
S2325` gets `CA1822`, `[SuppressMessage("...", "S1481:...")]` a sibling for `IDE0059`, `<NoWarn>` the replacing ids, so
code where a team suppressed a Sonar rule doesn't get the fix (`init --write` does this too when it finds a Sonar
setup). Limits: sub-directory `.editorconfig` files aren't read; Sonar's rule parameters (`SonarLint.xml`) aren't read.

## Modernizing afterwards

StyleCop has no rules for newer C# or APIs, so the migration turns none on. To add the SDK's modernization rules
(their own block, kept when you run `stylebro-migrate --write` again): `stylebro-migrate init --modernize --write`,
see [modernizing.md](modernizing.md).

## Limits

- Per-project rulesets and global configs (`<CodeAnalysisRuleSet>` in a test project's props) can't be mapped to
  paths; the strictest one wins. Add the test project's exceptions to a `.editorconfig` in its directory.
- StyleCop's XML file header (`<copyright file=...>`, the default for SA1633) becomes [BRO1615](rules/BRO1615.md) with
  stylecop.json's `companyName`, `copyrightText` (custom `variables` filled in) and `headerDecoration`; a plain
  header (`"xmlHeader": false`) becomes IDE0073's `file_header_template`. BRO1615 is one rule for SA1633-SA1641, so
  it's off when any of them is off. With a plain header StyleCop's SA1633 only asks for a header and SA1636 compares
  its text (line by line, ignoring leading and trailing spaces; only while SA1635 is on too). IDE0073 always compares
  the text, so it's on only when SA1633, SA1635 and SA1636 all are.
