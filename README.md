# StyleBro

Roslyn analyzers and code fixes that keep C# code tidy. It's a modern alternative to StyleCop, built so that **`dotnet format` fixes everything it reports**.

> StyleCop writes you a ticket. StyleBro just fixes it. Rule IDs use the `BRO` prefix.

**New here?** [Getting started](docs/getting-started.md): from install to the first `dotnet format` run, for a new
project or coming from StyleCop.

**Deciding whether to switch from StyleCop?** [StyleBro vs StyleCop](docs/stylebro-vs-stylecop.md): coverage, speed,
correctness and migration, with measured numbers.

## Principles

- **Every rule has a code fix, and Fix All works.** Fixes are written for `dotnet format`: deterministic, idempotent, and they converge in a single pass. CI checks this with a second `dotnet format --verify-no-changes` run.
- **Don't duplicate the SDK.** If a built-in `IDE` rule already covers a StyleCop rule, StyleBro uses that rule instead of shipping a copy: the preset sets its options and `stylebro-migrate init` turns it on in `.editorconfig`.
- **Configurable through `.editorconfig`**, with a recommended preset shipped as a low-priority global config.

## Rules

| ID | Title | Replaces | Fix |
|----|-------|----------|-----|
| [BRO1001](docs/rules/BRO1001.md) | Members should be ordered | SA1201-SA1204, SA1214 | Yes, with custom Fix All |
| [BRO1002](docs/rules/BRO1002.md) | Single-line comments should begin with a space | SA1005 | Yes |
| [BRO1003](docs/rules/BRO1003.md) | Property accessors should follow order | SA1212 | Yes |
| [BRO1004](docs/rules/BRO1004.md) | Event accessors should follow order | SA1213 | Yes |
| [BRO1005](docs/rules/BRO1005.md) | Documentation lines should begin with single space | SA1004 | Yes |
| [BRO1006](docs/rules/BRO1006.md) | Preprocessor keywords should not be preceded by a space | SA1006 | Yes |
| [BRO1007](docs/rules/BRO1007.md) | Partial elements should declare an access modifier | SA1205 | Yes |
| [BRO1101](docs/rules/BRO1101.md) | Code should not contain empty statements | SA1106 | Yes |
| [BRO1102](docs/rules/BRO1102.md) | Each attribute should be in its own brackets | SA1133 | Yes |
| [BRO1103](docs/rules/BRO1103.md) | Constants should be on the right-hand side of comparisons | SA1131 | Yes |
| [BRO1104](docs/rules/BRO1104.md) | Use default instead of a value type's default constructor | SA1129 | Yes |
| [BRO1105](docs/rules/BRO1105.md) | Constructor initializers should be on their own line | SA1128 | Yes |
| [BRO1106](docs/rules/BRO1106.md) | Use string.Empty for empty strings | SA1122 | Yes |
| [BRO1107](docs/rules/BRO1107.md) | Split parameters should start on the line after the declaration | SA1116 | Yes |
| [BRO1108](docs/rules/BRO1108.md) | Parameters should be on the same line or on separate lines | SA1117 | Yes |
| [BRO1109](docs/rules/BRO1109.md) | Opening parenthesis or bracket should be on the declaration line | SA1110 | Yes |
| [BRO1110](docs/rules/BRO1110.md) | Closing parenthesis or bracket should be on the line of the last item | SA1111 | Yes |
| [BRO1111](docs/rules/BRO1111.md) | Generic type constraints should be on their own line | SA1127 | Yes |
| [BRO1112](docs/rules/BRO1112.md) | Do not use regions (off in the preset) | SA1124 | Yes |
| [BRO1113](docs/rules/BRO1113.md) | Regions should not be placed inside code elements | SA1123 | Yes |
| [BRO1114](docs/rules/BRO1114.md) | Do not combine fields | SA1132 | Yes |
| [BRO1115](docs/rules/BRO1115.md) | Use shorthand for nullable types | SA1125 | Yes |
| [BRO1116](docs/rules/BRO1116.md) | Closing parenthesis should be on line of opening parenthesis | SA1112 | Yes |
| [BRO1117](docs/rules/BRO1117.md) | Comma should be on the same line as previous parameter | SA1113 | Yes |
| [BRO1118](docs/rules/BRO1118.md) | Parameter list should follow declaration | SA1114 | Yes |
| [BRO1119](docs/rules/BRO1119.md) | Parameter should follow comma | SA1115 | Yes |
| [BRO1120](docs/rules/BRO1120.md) | Comments should contain text | SA1120 | Yes |
| [BRO1121](docs/rules/BRO1121.md) | Enum values should be on separate lines | SA1136 | Yes |
| [BRO1122](docs/rules/BRO1122.md) | Use literal suffix notation instead of casting | SA1139 | Yes |
| [BRO1123](docs/rules/BRO1123.md) | Use tuple syntax | SA1141 | Yes |
| [BRO1124](docs/rules/BRO1124.md) | Refer to tuple elements by name | SA1142 | Yes |
| [BRO1125](docs/rules/BRO1125.md) | Use lambda syntax | SA1130 | Yes |
| [BRO1126](docs/rules/BRO1126.md) | Using directives should be qualified | SA1135 | Yes |
| [BRO1127](docs/rules/BRO1127.md) | Query clause should follow previous clause | SA1102 | Yes |
| [BRO1128](docs/rules/BRO1128.md) | Query clauses should be on separate lines or all on one line | SA1103 | Yes |
| [BRO1129](docs/rules/BRO1129.md) | Query clause should begin on new line when previous clause spans multiple lines | SA1104 | Yes |
| [BRO1130](docs/rules/BRO1130.md) | Query clauses spanning multiple lines should begin on own line | SA1105 | Yes |
| [BRO1131](docs/rules/BRO1131.md) | Do not prefix calls with base unless local implementation exists | SA1100 | Yes |
| [BRO1132](docs/rules/BRO1132.md) | Block statements should not contain embedded comments | SA1108 | Yes |
| [BRO1134](docs/rules/BRO1134.md) | Declarations should not contain embedded comments | (none; StyleCop issue #605) | Yes |
| [BRO1135](docs/rules/BRO1135.md) | Integer literal suffixes should be upper case | (none; Sonar S818) | Yes |
| [BRO1133](docs/rules/BRO1133.md) | Check for null in one form | (none; Roslynator RCS1248) | Yes |
| [BRO1143](docs/rules/BRO1143.md) | No 'else' after a branch that ends in a jump (off by default) | (none; Meziantou MA0071, Roslynator RCS1211) | Yes |
| [BRO1136](docs/rules/BRO1136.md) | A lambda's single parameter should not be in parentheses | (none; StyleCop issue #762) | Yes |
| [BRO1137](docs/rules/BRO1137.md) | Remove a redundant 'return;' or 'yield break;' | (none; StyleCop issue #760, Roslynator RCS1134) | Yes |
| [BRO1138](docs/rules/BRO1138.md) | A string literal should be a plain string when nothing needs more | (none; Roslynator RCS1214/RCS1192/RCS1262) | Yes |
| [BRO1139](docs/rules/BRO1139.md) | Write 'else if' on one line | (none; Roslynator RCS0041/RCS1006) | Yes |
| [BRO1140](docs/rules/BRO1140.md) | A record with an empty body should end with ';' | (none; Roslynator RCS1251) | Yes |
| [BRO1141](docs/rules/BRO1141.md) | Object creation with an initializer: parentheses in one style | (none; Roslynator RCS1050) | Yes |
| [BRO1142](docs/rules/BRO1142.md) | Do not combine local variables | (none; Roslynator RCS1081) | Yes |
| [BRO1145](docs/rules/BRO1145.md) | A class, struct or interface with an empty body should end with ';' (off by default) | (none; Meziantou MA0206) | Yes |
| [BRO1146](docs/rules/BRO1146.md) | Write 'record' without 'class' | (none; Meziantou MA0174) | Yes |
| [BRO1404](docs/rules/BRO1404.md) | Access modifier should be declared | SA1400 | Yes |
| [BRO1405](docs/rules/BRO1405.md) | Statement should not use unnecessary parenthesis | SA1119 | Yes |
| [BRO1406](docs/rules/BRO1406.md) | Arithmetic expressions should declare precedence | SA1407 | Yes |
| [BRO1407](docs/rules/BRO1407.md) | Conditional expressions should declare precedence | SA1408 | Yes |
| [BRO1408](docs/rules/BRO1408.md) | Remove a redundant base type | (none; Roslynator RCS1042) | Yes |
| [BRO1409](docs/rules/BRO1409.md) | Methods of internal types should be internal, not public (off by default) | (none; StyleCop issue #2981, never implemented) | Yes |
| [BRO1514](docs/rules/BRO1514.md) | Braces should not be omitted | SA1503 | Yes |
| [BRO1515](docs/rules/BRO1515.md) | Braces should not be omitted from multi-line child statement | SA1519 | Yes |
| [BRO1516](docs/rules/BRO1516.md) | Use braces consistently | SA1520 | Yes |
| [BRO1517](docs/rules/BRO1517.md) | Code should not contain multiple blank lines in a row | SA1507 | Yes |
| [BRO1518](docs/rules/BRO1518.md) | Closing braces should not be preceded by blank line | SA1508 | Yes |
| [BRO1519](docs/rules/BRO1519.md) | Closing brace should be followed by blank line | SA1513 | Yes |
| [BRO1520](docs/rules/BRO1520.md) | Place the operator consistently when an expression wraps | (none; Roslynator RCS0027/RCS0028) | Yes |
| [BRO1521](docs/rules/BRO1521.md) | Place '=>' consistently when an expression body wraps | (none; Roslynator RCS0032) | Yes |
| [BRO1522](docs/rules/BRO1522.md) | Place '=' consistently when an assignment wraps | (none; Roslynator RCS0052) | Yes |
| [BRO1523](docs/rules/BRO1523.md) | Each call of a split call chain starts its own line | (none; Roslynator RCS0054) | Yes |
| [BRO1525](docs/rules/BRO1525.md) | Attributes should not be followed by a blank line | (none; SA1521 proposed, never implemented) | Yes |
| [BRO1524](docs/rules/BRO1524.md) | A split conditional expression has the condition, '?' and ':' parts on their own lines | (none; StyleCop issue #651) | Yes |
| [BRO1526](docs/rules/BRO1526.md) | Blank line between switch sections | (none; Roslynator RCS0061) | Yes |
| [BRO1527](docs/rules/BRO1527.md) | Auto-accessors should be on one line | (none; Roslynator RCS0042) | Yes |
| [BRO1301](docs/rules/BRO1301.md) | Variable names should begin with a lower-case letter | SA1312 | Yes |
| [BRO1302](docs/rules/BRO1302.md) | Parameter names should begin with a lower-case letter | SA1313 | Yes |
| [BRO1303](docs/rules/BRO1303.md) | Private field names should be camelCase | SA1306, SA1309 | Yes |
| [BRO1304](docs/rules/BRO1304.md) | Interface names should begin with I | SA1302 | Yes |
| [BRO1305](docs/rules/BRO1305.md) | Type parameter names should begin with T | SA1314 | Yes |
| [BRO1306](docs/rules/BRO1306.md) | Constant, static readonly and non-private field names should be PascalCase | SA1303, SA1304, SA1307, SA1311 | Yes |
| [BRO1307](docs/rules/BRO1307.md) | Field names should not begin with a prefix | SA1308 | Yes |
| [BRO1308](docs/rules/BRO1308.md) | Field names should not contain an underscore | SA1310 | Yes |
| [BRO1309](docs/rules/BRO1309.md) | Element names should begin with an upper-case letter | SA1300 | Yes |
| [BRO1310](docs/rules/BRO1310.md) | Field names should not use Hungarian notation (off by default) | SA1305 | Yes |
| [BRO1311](docs/rules/BRO1311.md) | Tuple element names should use correct casing | SA1316 | Yes |
| [BRO1312](docs/rules/BRO1312.md) | Namespace names should begin with an upper-case letter (off by default) | SA1300 | Yes |
| [BRO1313](docs/rules/BRO1313.md) | Parameter names should match the base member (off by default) | (none; SA1315 proposed, never implemented; SDK CA1725) | Yes |
| [BRO1314](docs/rules/BRO1314.md) | Asynchronous method names should end with Async (off by default) | (none; Roslynator RCS1046, Meziantou MA0137) | Yes |
| [BRO1401](docs/rules/BRO1401.md) | Use a trailing comma in multi-line initializers | SA1413 | Yes |
| [BRO1402](docs/rules/BRO1402.md) | Attribute constructor should not use unnecessary parenthesis | SA1411 | Yes |
| [BRO1403](docs/rules/BRO1403.md) | Remove delegate parenthesis when possible | SA1410 | Yes |
| [BRO1601](docs/rules/BRO1601.md) | Overrides and implementations should inherit their documentation | SA1600 (these members) | Yes |
| [BRO1602](docs/rules/BRO1602.md) | Single-line comments should not use documentation style slashes | SA1626 | Yes |
| [BRO1603](docs/rules/BRO1603.md) | Documentation text should end with a period | SA1629 | Yes |
| [BRO1604](docs/rules/BRO1604.md) | Property summary documentation should match accessors | SA1623 | Yes |
| [BRO1605](docs/rules/BRO1605.md) | Property summary documentation should omit accessor with restricted access | SA1624 | Yes |
| [BRO1606](docs/rules/BRO1606.md) | Constructor summary documentation should begin with standard text | SA1642 | Yes |
| [BRO1607](docs/rules/BRO1607.md) | Destructor summary documentation should begin with standard text | SA1643 | Yes |
| [BRO1608](docs/rules/BRO1608.md) | Void return value should not be documented | SA1617 | Yes |
| [BRO1609](docs/rules/BRO1609.md) | Do not use placeholder elements | SA1651 | Yes |
| [BRO1610](docs/rules/BRO1610.md) | Documentation text should not be empty | SA1627 | Yes |
| [BRO1611](docs/rules/BRO1611.md) | Element parameter documentation should match element parameters | SA1612 | Yes |
| [BRO1612](docs/rules/BRO1612.md) | Element parameter documentation should declare parameter name | SA1613 | Yes |
| [BRO1613](docs/rules/BRO1613.md) | Generic type parameter documentation should match type parameters | SA1620 | Yes |
| [BRO1614](docs/rules/BRO1614.md) | Generic type parameter documentation should declare parameter name | SA1621 | Yes |
| [BRO1615](docs/rules/BRO1615.md) | File should have the XML copyright header (needs `stylebro_file_header_company`) | SA1633-SA1638, SA1640, SA1641 | Yes |
| [BRO1616](docs/rules/BRO1616.md) | Write the summary's tags consistently on their own lines or on the text's line | (none; Roslynator RCS1253, Meziantou MA0177/MA0211) | Yes |
| [BRO1617](docs/rules/BRO1617.md) | Write a cref's type arguments in braces | (none; StyleCop's proposed SA1653) | Yes |
| [BRO1618](docs/rules/BRO1618.md) | Write a C# keyword in documentation as `<see langword="..."/>` | (none; Meziantou MA0154) | Yes |
| [BRO1619](docs/rules/BRO1619.md) | Put the documentation elements in the standard order | (none) | Yes |
| [BRO1501](docs/rules/BRO1501.md) | Opening braces should not be preceded by a blank line | SA1509 | Yes |
| [BRO1502](docs/rules/BRO1502.md) | Chained blocks should not be preceded by a blank line | SA1510 | Yes |
| [BRO1503](docs/rules/BRO1503.md) | Opening braces should not be followed by a blank line | SA1505 | Yes |
| [BRO1504](docs/rules/BRO1504.md) | Single-line comments should be preceded by a blank line | SA1515 | Yes |
| [BRO1505](docs/rules/BRO1505.md) | Elements should be separated by a blank line | SA1516 | Yes |
| [BRO1506](docs/rules/BRO1506.md) | Single-line comments should not be followed by a blank line | SA1512 | Yes |
| [BRO1507](docs/rules/BRO1507.md) | Files should not end with blank lines | SA1518 | Yes |
| [BRO1508](docs/rules/BRO1508.md) | A block should not be on a single line | SA1501 | Yes |
| [BRO1509](docs/rules/BRO1509.md) | An element should not be on a single line | SA1502 | Yes |
| [BRO1510](docs/rules/BRO1510.md) | Accessors should all be single-line or all multi-line | SA1504 | Yes |
| [BRO1511](docs/rules/BRO1511.md) | Element documentation headers should not be followed by blank line | SA1506 | Yes |
| [BRO1512](docs/rules/BRO1512.md) | While-do footer should not be preceded by blank line | SA1511 | Yes |
| [BRO1513](docs/rules/BRO1513.md) | Element documentation header should be preceded by blank line | SA1514 | Yes |

How every StyleCop rule maps to StyleBro or the SDK: [docs/stylecop-mapping.md](docs/stylecop-mapping.md). The rules
StyleBro doesn't cover, why, and what it would take to add them: [docs/skipped-rules.md](docs/skipped-rules.md).
Where the preset and the rules differ from StyleCop's defaults, and how to get StyleCop's behavior back:
[docs/differences-from-stylecop.md](docs/differences-from-stylecop.md).

## Usage

```xml
<PackageReference Include="StyleBro.Analyzers" Version="0.1.0-alpha.8" PrivateAssets="all" />
```

Then, once per repository, turn on the built-in .NET rules StyleBro relies on (IDE0055 formatting, IDE0036
modifier order, ...). Their severities have to be in `.editorconfig`: `dotnet format` ignores severities
from a package's preset.

```
dotnet tool install --global StyleBro.Migrate --prerelease
stylebro-migrate init --write      # adds a block to .editorconfig
```

```
dotnet format                      # whitespace + style + analyzers, including StyleBro
dotnet format analyzers --diagnostics BRO1001
```

The preset (StyleBro's rule severities and the formatting options) comes with the package. To opt out and configure
everything yourself: `<StyleBroPreset>none</StyleBroPreset>`. Coming from StyleCop? Use `stylebro-migrate --write`
instead of `init` (below). Checking it in CI: [docs/ci.md](docs/ci.md).

Projects with several target frameworks: run `stylebro-migrate format` instead of `dotnet format` (same options). Plain
`dotnet format` crashes there on the SDK's formatting fix; the command runs it once per target framework (details in
[docs/ci.md](docs/ci.md)).

## Migrating from StyleCop

`stylebro-migrate` reads a repository's StyleCop setup (rulesets, global configs, `.editorconfig` files,
`stylecop.json`) and writes matching StyleBro and SDK settings into its `.editorconfig` files, so the switch doesn't
reformat code StyleCop was happy with. It also carries `#pragma warning disable SA…` and `[SuppressMessage]`
suppressions over to the replacing rules, and lists the StyleCop rules nothing enforces any more.
`dotnet tool install --global StyleBro.Migrate --prerelease`, then `stylebro-migrate path/to/repo`. See
[docs/migrating.md](docs/migrating.md).

## Baseline: fail only on new violations

In a codebase with many existing violations, `stylebro-migrate baseline` records them in `stylebro.baseline`. The
build, the IDE and `dotnet format` then ignore those (every StyleBro rule and the SDK rules `stylebro-migrate init`
turns on), so only new code has to follow the rules; a violation counts as new once its line is edited. See
[docs/baseline.md](docs/baseline.md).

## Repository layout

```
src/StyleBro.Analyzers     netstandard2.0, Microsoft.CodeAnalysis.CSharp only (no Workspaces, RS1038)
src/StyleBro.CodeFixes     netstandard2.0, code fixes + Fix All providers
src/StyleBro.Package       packs both DLLs into analyzers/dotnet/cs + build/ preset
src/StyleBro.Migrate       stylebro-migrate: StyleCop setup -> .editorconfig settings
tests/StyleBro.Tests       Microsoft.CodeAnalysis.Testing: diagnostics, single fix, Fix All, idempotence
samples/Messy           dotnet format integration sample (Input -> Expected)
samples/StyleCopMigration  a StyleCop project to migrate step by step (see its README)
scripts/verify-format.ps1
```

## Developing

```
dotnet test tests/StyleBro.Tests
pwsh scripts/verify-format.ps1
dotnet pack src/StyleBro.Package -c Release -o artifacts
```

Roslyn is pinned to 4.8.0 so the package loads in the .NET 8 SDK / VS 17.8 and newer.

## Roadmap

1. More StyleCop rules with safe fixes (the candidates in [docs/skipped-rules.md](docs/skipped-rules.md))
2. ~~Baseline support~~ ([docs/baseline.md](docs/baseline.md))
