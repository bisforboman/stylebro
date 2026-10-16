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
`.git/stylebro-preview.patch` (`--diff=file` for another name, `--keep` keeps the copy). The copy also gets the
StyleBro.Analyzers reference if the repository has none, next to StyleCop's (see [Swap the package](#swap-the-package));
StyleCop.Analyzers stays, but format only fixes StyleBro's
and the built-in rules' ids, so it changes nothing. In a StyleCop-clean repository the format part should be close to
empty. Details: [Preview first](getting-started.md#preview-first).

The tool is on nuget.org from 0.1.0-alpha.5, released with the analyzers under the same version (latest: 0.7.0-alpha.1;
[which version](getting-started.md#which-version) says how to build `main` instead). To run it straight from a clone: `dotnet run --project src/StyleBro.Migrate -- path/to/repo`.

Then swap the package (below) and run `stylebro-migrate format`. It runs `dotnet format` with only StyleBro's rules
and the built-in rules the block turns on (plus whitespace formatting, unless the block turns IDE0055 off: `dotnet
format`'s whitespace pass ignores that severity, so then only its style and analyzer passes run), once per target framework where projects
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
<GlobalPackageReference Include="StyleBro.Analyzers" Version="0.7.0-alpha.1" />
```

With central package management the version goes into the `Directory.Packages.props` that holds StyleCop's version,
which may be a folder's (`src/Directory.Packages.props`) rather than the root's; a `PackageReference` with a `Version`
there fails the restore (NU1008). `--diff` edits the same file: a `GlobalPackageReference` next to StyleCop's
`GlobalPackageReference` (in a folder's `Directory.Packages.props` too), a version-less `PackageReference` and a
`PackageVersion` otherwise. `--write` turns the preset off (`<StyleBroPreset>none</StyleBroPreset>`) only in the
`Directory.Build.props` files of projects StyleCop runs in: elsewhere no settings are written, so the preset applies.

A project that opts out of the StyleCop reference it inherits (`<PackageReference Remove="StyleCop.Analyzers" />`, often
an analyzer or source generator project) needs the same for StyleBro, else it gets StyleBro, and when it turns central
package management off itself, the version-less reference fails its restore (NU1015):

```xml
<PackageReference Remove="StyleCop.Analyzers" />
<PackageReference Remove="StyleBro.Analyzers" />
```

The migration's report names the files that reference StyleCop and the ones that remove it, and `--diff` makes both
changes in its copy. A project that turns central package management off but keeps the inherited reference gets a
second, versioned reference instead ([getting-started.md](getting-started.md#1-add-the-package) shows the pair);
`--diff` writes that pair too.

**Old-style projects** (`packages.config`, StyleCop as `<Analyzer Include="..\packages\StyleCop.Analyzers.1.0.2\...">`):
removing StyleCop means removing its `packages.config` entry and the `<Analyzer>` items. StyleBro goes in the same way:
a `packages.config` entry, its `<Analyzer>` items and the import of its build targets, not a `PackageReference`.
Running `format` there takes extra steps, and `--diff` leaves those projects out:
[getting-started.md](getting-started.md#old-style-non-sdk-projects).

### StyleCop referenced in a git submodule

Some repositories get StyleCop from a shared submodule, such as SixLabors' `shared-infrastructure`, whose
`SixLabors.Global.props` the repository's `Directory.Build.props` imports. The submodule belongs to another repository,
so it can't be edited here. The report names the file and says where the swap goes instead: in the repository's own
`Directory.Build.props`, after the line that imports the submodule's file.

```xml
<Import Project="$(MSBuildThisFileDirectory)shared-infrastructure\msbuild\props\SixLabors.Global.props" />

<!-- StyleBro replaces StyleCop (referenced in the shared infrastructure). -->
<ItemGroup>
  <PackageReference Remove="StyleCop.Analyzers" />
  <PackageReference Include="StyleBro.Analyzers" Version="0.7.0-alpha.1" PrivateAssets="all" />
</ItemGroup>
```

After the swap, a second `--write` recognizes the migrated setup from the StyleCop version the first one recorded in
the block. It doesn't treat the setup as leftovers. Code that a project compiles from the submodule is not reported:
StyleBro can't fix it there, and `format` skips submodules. The package's build targets read the submodule paths from
`.gitmodules`.

`stylecop.json` and the StyleCop suppressions can stay; StyleBro ignores them. The steps in order:
[getting-started.md](getting-started.md#b-coming-from-stylecop). A small StyleCop project migrated step by step, with
real output: [samples/StyleCopMigration](https://github.com/bisforboman/stylebro/blob/main/samples/StyleCopMigration/README.md).

## What it reads

- **Only where StyleCop runs.** When some projects reference StyleCop.Analyzers (in the project, the
  `Directory.Build.props`/`.targets` or `Directory.Packages.props` it gets, or what those import) and others don't (tests,
  samples, benchmarks, a project with a `Remove` item), the settings apply only to the folders of the projects that run
  it, with the rules it had on there: the block's section names those folders (`[src/{App,App.Core}/**.cs]`; the
  topmost folders with no project without StyleCop below, and sibling folders sharing a name start no such project has
  as one wildcard, `src/{LiteBus,LiteBus.Inbox*}`), a
  sub-directory `.editorconfig` that covers all of them counts as the base (a `src/.editorconfig` that turns everything
  but documentation off), and folders without StyleCop get no settings at all; the report lists them, and so does a
  comment line at the top of the block (`# No StyleCop ran in: examples; ...`), which tells `stylebro-migrate format` to
  leave those folders alone: its whitespace pass would reformat them whatever the rule severities say. Remove that line
  to have them formatted. StyleBro's rules still report there in the build (at the preset's or their default
  severities), since no settings turn them off. Whether StyleCop runs in a project is MSBuild's answer: with several
  projects, each one is evaluated (`dotnet msbuild -getItem`, in parallel, a few seconds in all); only a project MSBuild
  can't evaluate falls back to reading the files' text, with a warning. When every project or none
  references StyleCop, the settings apply to the whole repository.
- StyleCop's own defaults (from the StyleCop 1.2 DLL's rule list).
- `*.ruleset` files, then `*.globalconfig` files, then the root `.editorconfig`'s sections for all C# files. Later
  sources win, like in the compiler: `dotnet_diagnostic.SAxxxx.severity` and
  `dotnet_analyzer_diagnostic.category-StyleCop.CSharp.*.severity` keys, and ruleset `<Rule Id="SAxxxx">` actions.
  Also like in the compiler, a bulk setting (a category, or `dotnet_analyzer_diagnostic.severity` for every rule)
  doesn't override a rule's own entry in a ruleset or another config. A ruleset's `<Include>`d rulesets count, under
  its own entries. When several rulesets or global configs disagree (one per project type), the strictest wins, a file
  that doesn't mention a rule counting with StyleCop's default, so production code keeps everything StyleCop enforced
  there.
- **Each project's own ruleset and `stylecop.json`.** A project's ruleset is the `<CodeAnalysisRuleSet>` its project
  file, its `Directory.Build.props`/`.targets` or what they import select (the last one counts), its `stylecop.json` the
  one it adds as `<AdditionalFiles>`. Only projects that run StyleCop count: a sample project without StyleCop (and so
  without a ruleset) doesn't bring StyleCop's defaults back. The repository-wide settings take the strictest of those
  rulesets and the `stylecop.json` most projects use; projects whose ruleset or `stylecop.json` gives other settings get
  a section of their own in the root `.editorconfig` (`[tests/**.cs]` with BRO1001 off where the tests' ruleset turns
  ordering off, `[src/**.cs]` with src's own license header). Projects that share a folder but not their settings can't
  be told apart by a section: the report warns and names the project whose settings the folder gets.
- Settings the repository's MSBuild files point to are read wherever they are, also inside a git submodule (a shared
  infrastructure repository): imported `.props`/`.targets` (for `GenerateDocumentationFile` and the StyleCop version),
  the rulesets they select, and a `stylecop.json` added as `<AdditionalFiles>`. They're only read.
- Sub-directory `.editorconfig` files and path-specific sections (like `[tests/**.cs]`) are translated in place: they
  get the settings that differ from the repository-wide ones, in the same file and section. A folder that turns every
  analyzer off (`dotnet_analyzer_diagnostic.severity = none`, common for vendored code) gets every replacing rule as
  `none`: a rule the root turns on by its id would win over the folder's bulk setting. A folder whose `.editorconfig`
  says `root = true` gets a block of its own, judged from its own code; one without C# files (an npm app) gets none.
- Git submodules and other nested repositories (folders with their own `.git` file or folder) are otherwise skipped:
  their settings, code and project files belong to another repository, and nothing in them is changed
  (`stylebro-migrate format` excludes them too). Files a project
  links from a submodule are another matter: see [excluding vendored code](getting-started.md#excluding-vendored-code).
- The StyleCop.Analyzers version: from a `PackageReference` or `PackageVersion`, a `packages.config` entry, or an
  `<Analyzer>` hint path into a `packages` folder (old-style projects). Rules the version's DLL doesn't have count as
  off: with 1.1.x the rules added in 1.2 (SA1141, SA1142, SA1316, SA1414), with 1.0.x also SA1135, SA1136, SA1137,
  SA1139, SA1314 and SA1413. 1.1.x's SA1413 doesn't check switch expressions yet: the migration writes
  `stylebro_trailing_comma_switch_expressions = false`, so [BRO1401](rules/BRO1401.md) leaves them alone too.
- StyleCop spacing rules that are off while others are on (kafkaflow: SA1000, SA1003, SA1009): IDE0055 enforces every
  SDK spacing option, so the code decides, as in `init`. A clear other form gets the option that keeps it
  (`csharp_space_after_keywords_in_control_flow_statements = false` for `if(x)`, `csharp_space_around_binary_operators`
  `none`, or `ignore` when the operators are mixed, `csharp_space_after_cast`, the parentheses options); where no option
  expresses the code (`new ()`, mixed `if(x)`/`if (x)`, a few outliers), IDE0055 stays on and the report names what
  `format` changes.
- Whether any project sets `GenerateDocumentationFile`. Without it the build doesn't parse XML documentation and
  StyleCop's rules that read it never run (it reports SA0001 instead), but `dotnet format` does parse it. So the
  StyleBro rules that read documentation as XML (BRO1603-BRO1611) stay off.
- StyleCop's alternative rules: SX1101 (no `this.`) turns on the SDK's IDE0003, SX1309 (fields begin with `_`) sets
  BRO1303 to `_camelCase`, which also covers SX1309S (private static fields begin with `_`; on its own, with instance
  fields named `count`, the report lists it as not expressible: SA1309 is off then, so StyleCop accepts instance fields
  with and without `_`, which BRO1303 would unify), and SA1412 (UTF-8 with BOM) writes `charset = utf-8-bom`.
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
  [BRO1306](rules/BRO1306.md)), and private static fields that aren't readonly to BRO1303's style
  (`stylebro_private_mutable_static_field_naming`): without these keys BRO1303 and BRO1306 would follow a camel-case
  `dotnet_naming_rule` for static fields that StyleCop never enforced (IDE1006 off), and rename fields StyleCop was
  happy with. With SA1309 off, `stylebro_allow_leading_underscore_in_pascal_fields = true` keeps constants, `static
  readonly` and non-private fields that start with `_` (`_defaultSelector`): StyleCop's PascalCase rules only check
  that the first character isn't lower-case, and the underscore is SA1309's.
- **SDK rules.** The rules StyleBro relies on (IDE0055 formatting, IDE0036 modifier order, IDE0049 type aliases, and so
  on) get the strongest severity of the StyleCop rules they cover, with options from `stylecop.json`.
- **Every rule by its id.** Each StyleBro rule gets its own `dotnet_diagnostic.BROxxxx.severity` line, `none` included
  (also the rules beyond StyleCop and those off by default): a bulk `dotnet_analyzer_diagnostic.severity = warning`
  in your `.editorconfig` would otherwise turn them on.
- **File header.** With StyleCop's XML header and a `companyName`, [BRO1615](rules/BRO1615.md) is on and IDE0073 off;
  with `xmlHeader: false`, IDE0073 (with `file_header_template`) and BRO1615 off. Never both: each would add its
  header above the other's on every run. Without a `companyName` BRO1615 stays off (StyleCop's default company,
  `PlaceholderCompany`, is nobody's real header). An `.editorconfig` value ends at a `;` or `#` and loses its leading
  spaces, so the header settings are written with escapes BRO1615 reads back: `\n` for a line break, `\u003B` for `;`,
  `\u0023` for `#`, `\u005C` for a backslash, `\u0020` for a space at either end (a GPL text's `WARRANTY;` used to cut
  the rest of the license off). IDE0073's `file_header_template` has no escapes: a plain header with a `;` or `#` leaves
  IDE0073 off, and the report says why.
- **Line endings.** `end_of_line` when your C# files clearly use one ending (judged like `init` does; files git
  converts on checkout don't count, and when they're the majority nothing is written) and your `.editorconfig`
  doesn't set it: without it `format` writes the OS's ending into the lines it changes. When your `.editorconfig` sets
  `end_of_line` but git converts the files on checkout (a `.gitattributes` `text` attribute without `eol`, or
  `core.autocrlf`), the endings are kept out of it: `end_of_line = unset` for C# files and IDE0055 as a suggestion (not
  the error SA1500 would give it), so a Windows checkout neither fails the build nor gets rewritten; the report says how
  to settle it.
- **CSharpier.** When the repository formats with [CSharpier](https://csharpier.com) (detected like `init` does, see
  [getting started](getting-started.md)), IDE0055 and the same layout rules `init` turns off are off, so the two
  formatters don't undo each other. The StyleCop rules only those replaced (SA1000-SA1028, SA1116, SA1117, ...) are
  listed in the report under "CSharpier decides it" instead of being enforced.
- **The StyleCop version** is recorded in the block (`# StyleCop.Analyzers version: ...`), so running `--write` again
  after removing the package still leaves the rules that version didn't have off.
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
can't take a key back from the preset. Using order is the clearest case: `dotnet format` sorts `using` directives whenever
`dotnet_sort_system_directives_first` is set, even to `false` (so the preset doesn't set it), and the block sets it only when StyleCop sorted usings
(SA1208 or SA1210 on), and where a sub-directory or path section has both off, the keys go only into the sections that
sort (a scope can't unset the main block's keys). SDK settings your root
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
again adds nothing. Only files where StyleCop ran get them (when only some projects reference it): elsewhere the
generated settings turn no rule on. A suppression gets every rule that replaces the StyleCop one, also one that can't report on the
code it covers (SA1300 around a local function gets BRO1312, the namespace rule, too): an id that doesn't apply is
harmless, and the suppression stays right if the code changes. Rules the generated settings turn off get none (an
analyzer like StyleSharp's SST1462 reports a suppression of a rule that never reports). The count the tool prints is of rule ids added (a
`disable`/`restore` pair counts twice), and the preview counts the same way. Sonar's suppressions get the same treatment ([Coming from SonarQube](#coming-from-sonarqube)).

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

## Coming from StyleSharp

[StyleSharp.Analyzers](https://www.nuget.org/packages/StyleSharp.Analyzers) is a StyleCop successor with `SST` ids
(`SST1309`) and `stylesharp.*` settings in `.editorconfig`. `stylebro-migrate` reads it the way it reads StyleCop: a
project that references `StyleSharp.Analyzers` (a `PackageReference`, a central `PackageVersion`, an `<Analyzer>` item or
`packages.config`) counts as running StyleCop, and each SST rule's severity (from `.editorconfig`, `.globalconfig` and
rulesets, plus StyleSharp's own defaults) counts as the StyleCop rule it stands for. Most SST rules kept StyleCop's
number (`SST1413` is SA1413); the exceptions:

| StyleSharp | Read as |
| --- | --- |
| SST1117 (`this.` as `stylesharp.instance_member_qualification` says) | SA1101 with `require_this`, StyleCop's SX1101 (no `this.`) with `omit_this`, StyleSharp's default |
| SST1309 (private fields are `_camelCase`) | StyleCop's SX1309 and SX1309S: BRO1303 keeps the `_` (`_camelCase`); SST1306 doesn't count then |
| SST1150-SST1171 (one per kind of list) | SA1117, on when all of them are |
| SST1214 and SST1215 (static and instance readonly fields first) | SA1214, on when both are |
| SST1633 (a header from `file_header_template`) | SA1633 with a plain header (IDE0073); nothing without `file_header_template` |
| SST1108, SST1116, SST1119 (other rules at those numbers) | nothing; StyleCop's SA1108, SA1116 and SA1119 count as off |

StyleSharp doesn't read `stylecop.json`, so neither does the migration in a StyleSharp repository (a leftover one is
named in the report). Its settings come from the `stylesharp.*` keys of the root `.editorconfig`'s sections for all C#
and the global configs: `instance_member_qualification`, `tuple_element_naming`, `allowed_hungarian_prefixes`,
`document_exposed_elements`/`document_internal_elements`/`document_private_elements`, and `file_header_template`; usings
go outside the namespace (SST1200). Suppressions of SST ids (`#pragma`, `[SuppressMessage]`, `<NoWarn>`) are carried over
to the rules that replace the StyleCop rules they stand for, like StyleCop's. The block records the StyleSharp version
(`# StyleSharp.Analyzers version:`), so after the package swap a second `--write` still reads the setup as StyleSharp's.

**Where StyleSharp's rule means something else.** Several SST rules kept StyleCop's number but check less, or check
differently (owner's decision 2026-10-16: match StyleSharp's behaviour). For a StyleSharp repository (without StyleCop
next to it) the migration writes StyleSharp's meaning:

| StyleSharp | StyleCop | Written |
| --- | --- | --- |
| SST1623/SST1624: a `protected` setter is restricted, a getter counts whatever its modifier, `Gets or sets` on a read-only property is fine | SA1623/SA1624 | `stylebro_summary_wording = stylesharp` ([BRO1604](rules/BRO1604.md)-[BRO1607](rules/BRO1607.md)) |
| SST1642/SST1643: only the words before the type count; static constructors aren't checked | SA1642/SA1643 | the same key |
| SST1201: methods, operators and conversions are one kind; nested records come after classes; a namespace's types aren't ordered | SA1201 | `stylebro_member_order = ..., conversion\|operator\|extension\|method, struct, class, record`, `stylebro_member_order_namespaces = false` ([BRO1001](rules/BRO1001.md)) |
| SST1305: also allows `db`, `ef`, `gc`, `id`, `io`, `js`, `ms`, `my`, `ok`, `so`, `ui`, `up`, `us` | SA1305 | `stylebro_allowed_hungarian_prefixes` ([BRO1310](rules/BRO1310.md)) |
| SST1413: no switch expressions, no `with` initializers | SA1413 | `stylebro_trailing_comma_switch_expressions = false`, `stylebro_trailing_comma_with_expressions = false` ([BRO1401](rules/BRO1401.md)) |
| SST1501/SST1502: empty `{ }` may stay on one line | SA1501/SA1502 | `stylebro_allow_empty_single_line_blocks = true` |
| SST1106: a `;` after a type's `}` is fine | SA1106 | `stylebro_allow_semicolon_after_closing_brace = true` |
| SST1629: the period only in summary, remarks, returns and value, and not after an element at the end | SA1629 | `stylebro_exclude_from_punctuation_check` with `param`, `typeparam`, `exception` and the inline elements (`see`, `c`, `paramref`, ...) |
| SST1300: no namespaces | SA1300 | [BRO1312](rules/BRO1312.md) off |

Known differences left (StyleBro changes code StyleSharp accepts; nothing StyleSharp then reports, checked with
`scripts/stylecop-survey/Compare-WithStyleCop.ps1 -StyleSharp` on every parity set): SST1131 only counts literals as
constants (BRO1103 also swaps `Max == x`, `string.Empty == s`); SST1129 skips target-typed `new()` (BRO1104 doesn't);
SST1312 and SST1305 check local declarations, `foreach` variables and parameters only (BRO1301/BRO1310 also `out var`,
patterns, `catch` and query variables); SST1300 doesn't check local functions (BRO1309 does); SST1407 counts `%` with
`*` and `/`, SST1408 doesn't check `and`/`or` patterns (BRO1406/BRO1407 do); SST1503/SST1519 accept an unbraced `if`
child (BRO1514/BRO1515 brace it); SST1516 doesn't check usings, accessors or a member after an `#if` line (BRO1505
does); SST1201 skips explicit interface implementations (BRO1001 ranks them public); SST1600 doesn't ask for
documentation on overrides (BRO1601 adds `<inheritdoc/>`); SST1150-SST1171 let an item start where the previous
multi-line item ends (BRO1108 doesn't); SST1518 requires a final newline (BRO1507 allows none: set
`insert_final_newline = true`); SST1514 accepts a doc comment right after `#endif` or a region line (BRO1513 adds a blank
line); SST1502 doesn't check accessor lists, event accessors or namespaces on one line (BRO1509 expands them);
SST1612/SST1620 accept tags out of parameter order (BRO1611/BRO1613 reorder them); a few rarer cases in
SST1139/SST1141/SST1142/SST1135/SST1103/SST1004/SST1617/SST1642 that the probe lists. SST1310 (off by default) also reports a leading `_`, so it contradicts SST1309;
with both on, BRO1303 follows SST1309.

The report lists the StyleSharp rules that are on and stand for no StyleCop rule (digit separators, complexity limits,
...): StyleBro doesn't replace them, so they stop when StyleSharp.Analyzers is removed. When StyleCop.Analyzers is
referenced too, a StyleCop rule counts as on when either package enforces it, and `stylecop.json`'s settings are used.

Data: `scripts/stylecop-survey/data/inventory-stylesharp-5.1.2.csv` (StyleSharp's rules, `Get-Inventory.ps1 -Package
StyleSharp.Analyzers`) and `stylesharp-mapping.csv` (written by `New-StyleSharpMapping.ps1`, which holds the exceptions).

**Using order.** When your `.editorconfig` sets `dotnet_sort_system_directives_first` (or
`dotnet_separate_import_directive_groups`), `dotnet format` sorts usings whatever the value. Files that aren't in that
order while your ordering rule (SA1208/SA1210, or StyleSharp's SST1208/SST1210, which compares case-sensitively) is on
and would report their sorted order are named in the report and in the block (`# Usings kept as they are ...`), and
`stylebro-migrate format` leaves them out, so it doesn't write what that rule reports. Files whose sorted order the rule
accepts (`Microsoft.*` above `System` under SA1210 alone: System first is fine with `systemUsingDirectivesFirst`) are
sorted, and the report says how many. Remove the line once that rule is gone to have them
sorted. The key itself is yours and stays.

**Settings without a package.** StyleCop or StyleSharp settings (`stylecop.json`, `SA`/`SST` severities) that no project
uses describe nothing the build enforces: the migration writes nothing and points to `stylebro-migrate init`, also when
the repository has a Sonar setup (init follows that too, and the code's own conventions).

## Coming from SonarQube

Many teams run SonarQube or SonarCloud next to (or instead of) StyleCop. `stylebro-migrate` and `stylebro-migrate init`
read that setup too and turn on the StyleBro and .NET rules that fix what the team's Sonar rules report, so
`stylebro-migrate format` cleans up findings Sonar can only list. Sonar stays: StyleBro doesn't turn any Sonar rule off,
and Sonar keeps reporting its own ids.

What it reads, in this order (later wins, like for StyleCop):

- **The base.** A quality profile exported from the server, when you pass one: `--sonar-profile profile.xml` (the XML from
  the server's `api/qualityprofiles/backup?language=cs&qualityProfile=<name>`, or Quality Profiles > Back up in the UI;
  its `csharpsquid` rules are on). Without it, when a project references `SonarAnalyzer.CSharp` (`PackageReference`,
  `GlobalPackageReference`, central package versions or `packages.config`), the package's own defaults ("Sonar way", from
  SonarAnalyzer.CSharp 10.35's rule list, adjusted to the referenced version for the rules in the mapping: S8969 only from
  10.30, S2325 on by default only from 9.32). A rule the referenced version doesn't have counts as off, also when a
  ruleset or config sets it.
- **Rulesets** with Sonar rules (`<Rules AnalyzerId="SonarAnalyzer.CSharp">`): your own, SonarLint's
  `.sonarlint/*csharp.ruleset`, and the scanner's `.sonarqube/conf/Sonar-cs.ruleset` when it's there (rulesets named
  `*none*` are skipped).
- **Global configs** (`dotnet_diagnostic.Sxxxx.severity`); with several (one per project type) the strictest wins.
- **The root `.editorconfig`'s** sections for all C# files.
- **Sub-directory `.editorconfig` files and path sections** (like `[tests/**.cs]`) with Sonar severities are scopes, like
  StyleCop's: each gets the fixing rules' lines that differ there, in its own file and section, so a Sonar rule lowered
  for the tests (`S3878 = suggestion`) lowers the rule that fixes it there too, and one turned off turns it off. `init`
  writes them as sections of its block in the root `.editorconfig`.

Without a Sonar package, a profile or Sonar rule ids in those files, nothing Sonar-related happens.

The Sonar rules that are on turn on these rules, at the strongest severity of the Sonar rules behind each. A mapping never
reports what the Sonar rule itself accepts (owner's policy): a rule that is stricter than Sonar's and can't be narrowed to it
isn't mapped.

| Sonar | Fixed by |
|---|---|
| S121 (braces) | [BRO1514](rules/BRO1514.md) with `csharp_prefer_braces = true` and `stylebro_braces_statements = if_and_loops` (not `using`, `lock`, `fixed` or a deconstructing `foreach`, like S121) |
| S818 (upper-case literal suffixes) | [BRO1135](rules/BRO1135.md) with `stylebro_upper_case_literal_suffixes = lone_l` (only a suffix that is just `l`, like S818) |
| S927 (parameter names match the base) | [BRO1313](rules/BRO1313.md) with `stylebro_match_base_parameter_names = non_generic_methods` (methods only, and not a parameter whose type in the base is a type parameter the method fills in, like S927) |
| S1066 (mergeable `if`s) | [BRO1149](rules/BRO1149.md) with `stylebro_allow_nested_dynamic_ifs = true` (conditions with a `dynamic` part aren't merged, like S1066) |
| S1116 (empty statements) | [BRO1101](rules/BRO1101.md) with `stylebro_allow_semicolon_after_closing_brace = true` (the `;` after a type's `}` stays, like S1116) |
| S1659 (one variable per declaration) | [BRO1142](rules/BRO1142.md) |
| S1939 (redundant base types) | [BRO1408](rules/BRO1408.md) |
| S2971 (LINQ predicate into `Count`/`Any`/...) | BRO1150 |
| S3878 (arrays for `params`) | [BRO1151](rules/BRO1151.md) with `stylebro_params_arrays = except_object_and_jagged` (not for `params object[]`, `params Array[]` or jagged arrays, like S3878) |
| S4136 (overloads together) | [BRO1001](rules/BRO1001.md)'s `stylebro_keep_overloads_together = same_access` (overloads with the same access, like S4136), only when BRO1001 is on |
| S8969 (redundant `!`) | [BRO1147](rules/BRO1147.md) with `stylebro_allow_null_forgiving_on_unannotated = true` (a `!` on a value declared without nullable annotations stays, like S8969) |

**Not covered** (the report lists each under "Not covered" with its reason; they stay Sonar's). The fixing rule reports what
the Sonar rule accepts, and has no setting that narrows it. (BRO1101, BRO1135, BRO1147, BRO1149, BRO1151 and BRO1514 got
one each for S1116, S818, S8969, S1066, S3878 and S121, the owner's decision of 2026-10-12; the .NET SDK's rules can't be
changed.)

| Sonar | Rule | What it reports that Sonar accepts |
|---|---|---|
| S100, S101 (PascalCase names) | BRO1309 | events, enum members, enums, delegates, names with `_` inside, test classes (`fooTests`) |
| S1118 (utility classes) | CA1052 | a private or protected constructor, a singleton member, a non-static nested type |
| S1125 (redundant boolean literals) | IDE0075, IDE0100 | constants and `default`, `c ? true : true`, a comparison inside a conditional with a throw |
| S1481, S1854 (unused locals and assignments) | IDE0059 | initial values like `0`, `null`, `""`, stores to captured variables, dead stores to used variables |
| S1905 (redundant casts) | IDE0004 | upcasts and numeric conversions Sonar doesn't call redundant (Autofac: `(Service)`, `(int)`) |
| S2325 (members that could be static) | CA1822 | members with attributes, empty and recursive methods, events, members of generic types |
| S3052 (initialized to the default value) | CA1805 | `default`, `new Guid()`, an enum set to `0`, a nullable value type set to `null` |
| S3260 (seal private classes) | CA1852 | its fix also seals private nested classes that have `protected` members, and every such member then gets CS0628 (SmartEnum: 144 warnings) |
| S3442 (public constructors of abstract classes) | CA1012 | a constructor in another part of a partial abstract class |

**Sonar's own warnings for S1066, S2971 and S3878** are fixed too when the project references SonarAnalyzer.CSharp:
BRO1149-BRO1151's fixes are registered for those ids, so `dotnet format` (or a light bulb) applies them to Sonar's
diagnostics, at the places the StyleBro rule would fix; where it skips the code (a comment in the merged text, an array of a
derived type, Sonar's other S2971 cases, ...) the Sonar warning stays. This needs the package in the build: findings only
the scanner reports in CI aren't seen by `dotnet format`. Checked against SonarAnalyzer.CSharp 10.35.0.4138
(`scripts/sonar-interop.ps1`). Sonar reports more than the StyleBro rules fix in places: S3878 on an array spread over several lines
(BRO1151 leaves those, see its page), S8969 on a `!` that hides a nested nullability mismatch or in a multi-targeted
project (BRO1147 leaves those); those warnings stay.

The data lives in the tool: `src/StyleBro.Migrate/data/sonar-mapping.tsv` (the mapping) and
`sonar-rules-10.35.0.4138.tsv` (Sonar's rule list and defaults, public metadata only).

**With StyleCop too**, a rule is on when either setup turns it on, at the stronger severity: the migration normally turns
the rules beyond StyleCop off (BRO1142, BRO1313, ...), but not when the matching Sonar rule is on. A setting
Sonar's rule needs replaces the one from the StyleCop setup, and the report lists each such conflict. A `stylebro_` setting that
only narrows a rule to the Sonar rule's cases isn't written when the StyleCop setup turns that rule on (SA1106 also wants
the `;` after a type's `}` gone, SA1503 braces on `using`): StyleCop's wider scope stays, and the report says so. An option of a rule StyleCop's setup leaves off
isn't applied: S4136 only sets an option of BRO1001, so with SA1201 or SA1202 off it stays Sonar's alone.

The report has a Sonar part: what was read, how many Sonar rules are on, which of them StyleBro or the SDK now fixes (and
with which rule), which mapped ones aren't applied and why, which have no safe fix (S3260, ...), and that the rest stay Sonar's (no safe automatic fix: unused
private members, cognitive complexity, ...). The generated block has the Sonar-driven lines last, under
`# From the SonarQube setup`, each with the Sonar rule's title. `--diff` prints the same part of the report before its
summary. `init` does the same in a repository without StyleCop, and prints it.

Suppressions of Sonar ids are carried over like StyleCop's ([Suppressions](#suppressions)): `#pragma warning disable
S1659` gets `BRO1142`, `[SuppressMessage("...", "S927:...")]` a sibling for `BRO1313`, `<NoWarn>` the replacing ids, so
code where a team suppressed a Sonar rule doesn't get the fix (`init --write` does this too when it finds a Sonar
setup). A Sonar rule that only sets an option is different: S4136 (overloads together) in a `<NoWarn>` gets a section
for that folder with `stylebro_keep_overloads_together = false`, and BRO1001 stays on there. Limits: Sonar's rule parameters (`SonarLint.xml`) aren't read.

## Modernizing afterwards

StyleCop has no rules for newer C# or APIs, so the migration turns none on. To add the SDK's modernization rules
(their own block, kept when you run `stylebro-migrate --write` again): `stylebro-migrate init --modernize --write`,
see [modernizing.md](modernizing.md).

## Limits

- Global configs (`.globalconfig`) apply to every project here; with several, the strictest wins. Per-project rulesets
  and `stylecop.json` files are mapped (above), except for projects that share a folder.
- StyleCop's XML file header (`<copyright file=...>`, the default for SA1633) becomes [BRO1615](rules/BRO1615.md) with
  stylecop.json's `companyName`, `copyrightText` (custom `variables` filled in) and `headerDecoration`; a plain
  header (`"xmlHeader": false`) becomes IDE0073's `file_header_template`. BRO1615 is one rule for SA1633-SA1641, so
  it's off when any of them is off. With a plain header StyleCop's SA1633 only asks for a header and SA1636 compares
  its text (line by line, ignoring leading and trailing spaces; only while SA1635 is on too). IDE0073 always compares
  the text, so it's on only when SA1633, SA1635 and SA1636 all are.
