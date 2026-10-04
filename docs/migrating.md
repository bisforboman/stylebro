# Migrating from StyleCop

Switching a StyleCop-clean repository to StyleBro shouldn't reformat code StyleCop was happy with. StyleBro's preset
follows StyleCop's defaults with a few deliberate exceptions ([differences-from-stylecop.md](differences-from-stylecop.md)),
but most teams have changed some of StyleCop's defaults anyway. `stylebro-migrate` reads what your team changed
and writes the matching settings.

```
dotnet tool install --global StyleBro.Migrate --prerelease
stylebro-migrate path/to/repo           # dry run: prints the report and the settings
stylebro-migrate path/to/repo --write   # writes them
```

The tool is on nuget.org from 0.1.0-alpha.5, released with the analyzers under the same version. From a clone of
this repository: `dotnet run --project src/StyleBro.Migrate -- path/to/repo`.

Then add the `StyleBro.Analyzers` package, remove `StyleCop.Analyzers`, and run `dotnet format`. The steps in order:
[getting-started.md](getting-started.md#b-coming-from-stylecop). A small StyleCop project migrated step by step, with
real output: [samples/StyleCopMigration](../samples/StyleCopMigration/README.md).

## What it reads

- StyleCop's own defaults (from the StyleCop 1.2 DLL's rule list).
- `*.ruleset` files, then `*.globalconfig` files, then the root `.editorconfig`'s sections for all C# files. Later
  sources win, like in the compiler: `dotnet_diagnostic.SAxxxx.severity` and
  `dotnet_analyzer_diagnostic.category-StyleCop.CSharp.*.severity` keys, and ruleset `<Rule Id="SAxxxx">` actions.
  When several rulesets or global configs disagree (one per project type), the strictest wins, so production code
  keeps everything StyleCop enforced there.
- Sub-directory `.editorconfig` files and path-specific sections (like `[tests/**.cs]`) are translated in place: they
  get the settings that differ from the repository-wide ones, in the same file and section.
- The StyleCop.Analyzers version in the project files: with 1.1.x, the rules added in 1.2 (SA1141, SA1142, SA1316,
  SA1414) count as off.
- Whether any project sets `GenerateDocumentationFile`. Without it the build doesn't parse XML documentation and
  StyleCop's rules that read it never run (it reports SA0001 instead), but `dotnet format` does parse it. So the
  StyleBro rules that read documentation as XML (BRO1603-BRO1611) stay off.
- StyleCop's alternative rules: SX1101 (no `this.`) turns on the SDK's IDE0003, SX1309 (fields begin with `_`) sets
  BRO1303 to `_camelCase`, and SA1412 (UTF-8 with BOM) writes `charset = utf-8-bom`.
- `stylecop.json`: `elementOrder`, `usingDirectivesPlacement`, `systemUsingDirectivesFirst`,
  `blankLinesBetweenUsingGroups`, `allowBuiltInTypeAliases`, indentation, the file header settings,
  `documentationCulture` and `excludeFromPunctuationCheck`.

## What it writes

A block between `# BEGIN stylebro-migrate` and `# END stylebro-migrate` in each `.editorconfig` it changes. Running the
tool again replaces the block, so put your own settings outside it.

- **StyleBro rules.** A rule is on only when StyleCop enforced every rule it replaces, with the weakest of their
  severities. [BRO1001](rules/BRO1001.md) replaces SA1201-SA1204 and SA1214 with one sort. A team that turned SA1201
  off doesn't get the full sort, so BRO1001 stays off and the report lists SA1203/SA1204 as partly covered. Rules
  that `stylecop.json` makes moot don't count: SA1203 when `elementOrder` leaves out `constant`, and so on.
- **Private field naming.** [BRO1303](rules/BRO1303.md) uses `_camelCase` when SA1309 (no leading underscore) is off
  and most of the repository's private fields start with `_`, and `camelCase` otherwise.
- **SDK rules.** The rules StyleBro relies on (IDE0055 formatting, IDE0036 modifier order, IDE0065 using placement, and so
  on) get the strongest severity of the StyleCop rules they cover, with options from `stylecop.json`.

- **Documentation scope.** `documentExposedElements`, `documentInternalElements` and `documentPrivateElements`
  become [BRO1601](rules/BRO1601.md)'s `stylebro_document_*` settings.
- **Documentation language and punctuation.** A `documentationCulture` other than English (`de-DE`, `fr-FR`; any
  `en-*` counts as English) turns off [BRO1604](rules/BRO1604.md)-[BRO1607](rules/BRO1607.md), which write English
  summary sentences (StyleCop checks translated ones); the report says so. `excludeFromPunctuationCheck`, when it
  differs from StyleCop's default (`["seealso"]`), becomes [BRO1603](rules/BRO1603.md)'s
  `stylebro_exclude_from_punctuation_check`.

Every key the preset sets is written, and `--write` turns the preset off (`<StyleBroPreset>none</StyleBroPreset>` in the
root `Directory.Build.props`, created if needed): the block replaces it. That matters because an `.editorconfig`
can't take a key back from the preset: `dotnet format` sorts `using` directives whenever
`dotnet_sort_system_directives_first` is set, even to `false`, so the block sets it only when StyleCop sorted usings
(SA1208 or SA1210 on). If a sub-directory has its own `Directory.Build.props` that doesn't import the root one, add
the property there too. SDK settings your root
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
again adds nothing.

## The report

The dry run lists the StyleCop rules that are on but that nothing enforces after the switch:

- **Partly covered or not expressible:** the StyleBro rule also enforces rules your team turned off (BRO1001 sorts by
  kind, accessibility and static together, so SA1203/SA1204 alone can't be carried over), or a setting can't be
  expressed for the SDK.
- **Dropped by design:** there's no safe automatic fix (the fix would be placeholder documentation, a file rename, or
  a changed public API).
- **Not covered yet:** candidates for future StyleBro rules. See [stylecop-mapping.md](stylecop-mapping.md).

## Limits

- Per-project rulesets and global configs (`<CodeAnalysisRuleSet>` in a test project's props) can't be mapped to
  paths; the strictest one wins. Add the test project's exceptions to a `.editorconfig` in its directory.
- StyleCop's XML file header (`<copyright file=...>`, the default for SA1633) becomes [BRO1615](rules/BRO1615.md) with
  stylecop.json's `companyName`, `copyrightText` (custom `variables` filled in) and `headerDecoration`; a plain
  header (`"xmlHeader": false`) becomes IDE0073's `file_header_template`. BRO1615 is one rule for SA1633-SA1641, so
  it's off when any of them is off.
