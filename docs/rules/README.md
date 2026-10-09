# Rules

Every StyleBro rule, by area. Each one has a code fix that `dotnet format` applies (Fix All included), and each page
has an example, the reasons, the configuration and how it differs from StyleCop.

**Default**: *on* means the rule reports as a warning with the package's preset; *off* rules are opt-in
(`dotnet_diagnostic.BROxxxx.severity = warning` in `.editorconfig`). `stylebro-migrate` sets each rule from your
StyleCop setup instead, and turns off the rules StyleCop doesn't have.

Where the rules and the preset differ from StyleCop: [differences-from-stylecop.md](../differences-from-stylecop.md).
How every StyleCop rule maps to StyleBro or the SDK: [stylecop-mapping.md](../stylecop-mapping.md), and the ones
StyleBro leaves out: [skipped-rules.md](../skipped-rules.md).

**Files from NuGet packages** aren't checked: a source package's files (`contentFiles`, or files a package's build
targets compile) live in the package folder, where nobody can fix them and `dotnet format` would edit the shared
package cache. The package passes the restore's package folders (`NuGetPackageFolders`) to `PackageFileSuppressor`,
which hides every StyleBro finding there; without them (no package build targets), a path with a `contentFiles`
folder counts. Shared source linked from elsewhere in the repository is still checked.

## Ordering, spacing and comments

| ID | Title | Replaces | Default |
|----|-------|----------|---------|
| [BRO1001](BRO1001.md) | Members should be ordered | SA1201-SA1204, SA1214 | on |
| [BRO1002](BRO1002.md) | Single-line comments should begin with a space | SA1005 | on |
| [BRO1003](BRO1003.md) | Property accessors should follow order | SA1212 | on |
| [BRO1004](BRO1004.md) | Event accessors should follow order | SA1213 | on |
| [BRO1005](BRO1005.md) | Documentation lines should begin with single space | SA1004 | on |
| [BRO1006](BRO1006.md) | Preprocessor keywords should not be preceded by a space | SA1006 | on |
| [BRO1007](BRO1007.md) | Partial elements should declare an access modifier | SA1205 | on |
| [BRO1008](BRO1008.md) | Using directives should be placed correctly | SA1200 | on |

## Readability

| ID | Title | Replaces | Default |
|----|-------|----------|---------|
| [BRO1101](BRO1101.md) | Code should not contain empty statements | SA1106 | on |
| [BRO1102](BRO1102.md) | Each attribute should be in its own brackets | SA1133 | on |
| [BRO1103](BRO1103.md) | Constants should be on the right-hand side of comparisons | SA1131 | on |
| [BRO1104](BRO1104.md) | Use default instead of a value type's default constructor | SA1129 | on |
| [BRO1105](BRO1105.md) | Constructor initializers should be on their own line | SA1128 | on |
| [BRO1106](BRO1106.md) | Use string.Empty for empty strings | SA1122 | on |
| [BRO1107](BRO1107.md) | Split parameters should start on the line after the declaration | SA1116 | on |
| [BRO1108](BRO1108.md) | Parameters should be on the same line or on separate lines | SA1117 | on |
| [BRO1109](BRO1109.md) | Opening parenthesis or bracket should be on the declaration line | SA1110 | on |
| [BRO1110](BRO1110.md) | Closing parenthesis or bracket should be on the line of the last item | SA1111 | on |
| [BRO1111](BRO1111.md) | Generic type constraints should be on their own line | SA1127 | on |
| [BRO1112](BRO1112.md) | Do not use regions | SA1124 | off in the preset |
| [BRO1113](BRO1113.md) | Regions should not be placed inside code elements | SA1123 | on |
| [BRO1114](BRO1114.md) | Do not combine fields | SA1132 | on |
| [BRO1115](BRO1115.md) | Use shorthand for nullable types | SA1125 | on |
| [BRO1116](BRO1116.md) | Closing parenthesis should be on line of opening parenthesis | SA1112 | on |
| [BRO1117](BRO1117.md) | Comma should be on the same line as previous parameter | SA1113 | on |
| [BRO1118](BRO1118.md) | Parameter list should follow declaration | SA1114 | on |
| [BRO1119](BRO1119.md) | Parameter should follow comma | SA1115 | on |
| [BRO1120](BRO1120.md) | Comments should contain text | SA1120 | on |
| [BRO1121](BRO1121.md) | Enum values should be on separate lines | SA1136 | on |
| [BRO1122](BRO1122.md) | Use literal suffix notation instead of casting | SA1139 | on |
| [BRO1123](BRO1123.md) | Use tuple syntax | SA1141 | on |
| [BRO1124](BRO1124.md) | Refer to tuple elements by name | SA1142 | on |
| [BRO1125](BRO1125.md) | Use lambda syntax | SA1130 | on |
| [BRO1126](BRO1126.md) | Using directives should be qualified | SA1135 | on |
| [BRO1127](BRO1127.md) | Query clause should follow previous clause | SA1102 | on |
| [BRO1128](BRO1128.md) | Query clauses should be on separate lines or all on one line | SA1103 | on |
| [BRO1129](BRO1129.md) | Query clause should begin on new line when previous clause spans multiple lines | SA1104 | on |
| [BRO1130](BRO1130.md) | Query clauses spanning multiple lines should begin on own line | SA1105 | on |
| [BRO1131](BRO1131.md) | Do not prefix calls with base unless local implementation exists | SA1100 | on |
| [BRO1132](BRO1132.md) | Block statements should not contain embedded comments | SA1108 | on |
| [BRO1133](BRO1133.md) | Check for null in one form | (none; Roslynator RCS1248) | on |
| [BRO1134](BRO1134.md) | Declarations should not contain embedded comments | (none; StyleCop issue #605) | on |
| [BRO1135](BRO1135.md) | Integer literal suffixes should be upper case | (none; Sonar S818) | on |
| [BRO1136](BRO1136.md) | A lambda's single parameter should not be in parentheses | (none; StyleCop issue #762) | on |
| [BRO1137](BRO1137.md) | Remove a redundant 'return;' or 'yield break;' | (none; StyleCop issue #760, Roslynator RCS1134) | on |
| [BRO1138](BRO1138.md) | A string literal should be a plain string when nothing needs more | (none; Roslynator RCS1214/RCS1192/RCS1262) | on |
| [BRO1139](BRO1139.md) | Write 'else if' on one line | (none; Roslynator RCS0041/RCS1006) | on |
| [BRO1140](BRO1140.md) | A record with an empty body should end with ';' | (none; Roslynator RCS1251) | on |
| [BRO1141](BRO1141.md) | Object creation with an initializer: parentheses in one style | (none; Roslynator RCS1050) | on |
| [BRO1142](BRO1142.md) | Do not combine local variables | (none; Roslynator RCS1081) | on |
| [BRO1143](BRO1143.md) | No 'else' after a branch that ends in a jump | (none; Meziantou MA0071, Roslynator RCS1211) | off |
| [BRO1144](BRO1144.md) | Escape identifiers that C# 14 reads as keywords | (none; Sonar S8367/S8368/S8380, no fix) | on |
| [BRO1145](BRO1145.md) | A class, struct or interface with an empty body should end with ';' | (none; Meziantou MA0206) | off |
| [BRO1146](BRO1146.md) | Write 'record' without 'class' | (none; Meziantou MA0174) | on |
| [BRO1147](BRO1147.md) | No redundant null-forgiving `!` | (none; Sonar S8969, IDE0370) | off |
| [BRO1148](BRO1148.md) | `x is not null` instead of `x.HasValue` | (none; Meziantou MA0171) | off |

## Naming

| ID | Title | Replaces | Default |
|----|-------|----------|---------|
| [BRO1301](BRO1301.md) | Variable names should begin with a lower-case letter | SA1312 | on |
| [BRO1302](BRO1302.md) | Parameter names should begin with a lower-case letter | SA1313 | on |
| [BRO1303](BRO1303.md) | Private field names should be camelCase | SA1306, SA1309 | on |
| [BRO1304](BRO1304.md) | Interface names should begin with I | SA1302 | on |
| [BRO1305](BRO1305.md) | Type parameter names should begin with T | SA1314 | on |
| [BRO1306](BRO1306.md) | Constant, static readonly and non-private field names should be PascalCase | SA1303, SA1304, SA1307, SA1311 | on |
| [BRO1307](BRO1307.md) | Field names should not begin with a prefix | SA1308 | on |
| [BRO1308](BRO1308.md) | Field names should not contain an underscore | SA1310 | on |
| [BRO1309](BRO1309.md) | Element names should begin with an upper-case letter | SA1300 | on |
| [BRO1310](BRO1310.md) | Field names should not use Hungarian notation | SA1305 | off |
| [BRO1311](BRO1311.md) | Tuple element names should use correct casing | SA1316 | on |
| [BRO1312](BRO1312.md) | Namespace names should begin with an upper-case letter | SA1300 | off |
| [BRO1313](BRO1313.md) | Parameter names should match the base member | (none; SA1315 proposed, never implemented; SDK CA1725) | off |
| [BRO1314](BRO1314.md) | Asynchronous method names should end with Async | (none; Roslynator RCS1046, Meziantou MA0137) | off |

## Maintainability

| ID | Title | Replaces | Default |
|----|-------|----------|---------|
| [BRO1401](BRO1401.md) | Use a trailing comma in multi-line initializers | SA1413 | on |
| [BRO1402](BRO1402.md) | Attribute constructor should not use unnecessary parenthesis | SA1411 | on |
| [BRO1403](BRO1403.md) | Remove delegate parenthesis when possible | SA1410 | on |
| [BRO1404](BRO1404.md) | Access modifier should be declared | SA1400 | on |
| [BRO1405](BRO1405.md) | Statement should not use unnecessary parenthesis | SA1119 | on |
| [BRO1406](BRO1406.md) | Arithmetic expressions should declare precedence | SA1407 | on |
| [BRO1407](BRO1407.md) | Conditional expressions should declare precedence | SA1408 | on |
| [BRO1408](BRO1408.md) | Remove a redundant base type | (none; Roslynator RCS1042) | on |
| [BRO1409](BRO1409.md) | Methods of internal types should be internal, not public | (none; StyleCop issue #2981, never implemented) | off |
| [BRO1410](BRO1410.md) | Patterns should not use unnecessary parentheses | (none; SA1119 checks expressions only) | on |

## Layout

| ID | Title | Replaces | Default |
|----|-------|----------|---------|
| [BRO1501](BRO1501.md) | Opening braces should not be preceded by a blank line | SA1509 | on |
| [BRO1502](BRO1502.md) | Chained blocks should not be preceded by a blank line | SA1510 | on |
| [BRO1503](BRO1503.md) | Opening braces should not be followed by a blank line | SA1505 | on |
| [BRO1504](BRO1504.md) | Single-line comments should be preceded by a blank line | SA1515 | on |
| [BRO1505](BRO1505.md) | Elements should be separated by a blank line | SA1516 | on |
| [BRO1506](BRO1506.md) | Single-line comments should not be followed by a blank line | SA1512 | on |
| [BRO1507](BRO1507.md) | Files should not end with blank lines | SA1518 | on |
| [BRO1508](BRO1508.md) | A block should not be on a single line | SA1501 | on |
| [BRO1509](BRO1509.md) | An element should not be on a single line | SA1502 | on |
| [BRO1510](BRO1510.md) | Accessors should all be single-line or all multi-line | SA1504 | on |
| [BRO1511](BRO1511.md) | Element documentation headers should not be followed by blank line | SA1506 | on |
| [BRO1512](BRO1512.md) | While-do footer should not be preceded by blank line | SA1511 | on |
| [BRO1513](BRO1513.md) | Element documentation header should be preceded by blank line | SA1514 | on |
| [BRO1514](BRO1514.md) | Braces should not be omitted | SA1503 | on |
| [BRO1515](BRO1515.md) | Braces should not be omitted from multi-line child statement | SA1519 | on |
| [BRO1516](BRO1516.md) | Use braces consistently | SA1520 | on |
| [BRO1517](BRO1517.md) | Code should not contain multiple blank lines in a row | SA1507 | on |
| [BRO1518](BRO1518.md) | Closing braces should not be preceded by blank line | SA1508 | on |
| [BRO1519](BRO1519.md) | Closing brace should be followed by blank line | SA1513 | on |
| [BRO1520](BRO1520.md) | Place the operator consistently when an expression wraps | (none; Roslynator RCS0027/RCS0028) | on |
| [BRO1521](BRO1521.md) | Place '=>' consistently when an expression body wraps | (none; Roslynator RCS0032) | on |
| [BRO1522](BRO1522.md) | Place '=' consistently when an assignment wraps | (none; Roslynator RCS0052) | on |
| [BRO1523](BRO1523.md) | Each call of a split call chain starts its own line | (none; Roslynator RCS0054) | on |
| [BRO1524](BRO1524.md) | A split conditional expression has the condition, '?' and ':' parts on their own lines | (none; StyleCop issue #651) | on |
| [BRO1525](BRO1525.md) | Attributes should not be followed by a blank line | (none; SA1521 proposed, never implemented) | on |
| [BRO1526](BRO1526.md) | Blank line between switch sections | (none; Roslynator RCS0061) | on |
| [BRO1527](BRO1527.md) | Auto-accessors should be on one line | (none; Roslynator RCS0042) | on |

## Documentation

| ID | Title | Replaces | Default |
|----|-------|----------|---------|
| [BRO1601](BRO1601.md) | Overrides and implementations should inherit their documentation | SA1600 (these members) | on |
| [BRO1602](BRO1602.md) | Single-line comments should not use documentation style slashes | SA1626 | on |
| [BRO1603](BRO1603.md) | Documentation text should end with a period | SA1629 | on |
| [BRO1604](BRO1604.md) | Property summary documentation should match accessors | SA1623 | on |
| [BRO1605](BRO1605.md) | Property summary documentation should omit accessor with restricted access | SA1624 | on |
| [BRO1606](BRO1606.md) | Constructor summary documentation should begin with standard text | SA1642 | on |
| [BRO1607](BRO1607.md) | Destructor summary documentation should begin with standard text | SA1643 | on |
| [BRO1608](BRO1608.md) | Void return value should not be documented | SA1617 | on |
| [BRO1609](BRO1609.md) | Do not use placeholder elements | SA1651 | on |
| [BRO1610](BRO1610.md) | Documentation text should not be empty | SA1627 | on |
| [BRO1611](BRO1611.md) | Element parameter documentation should match element parameters | SA1612 | on |
| [BRO1612](BRO1612.md) | Element parameter documentation should declare parameter name | SA1613 | on |
| [BRO1613](BRO1613.md) | Generic type parameter documentation should match type parameters | SA1620 | on |
| [BRO1614](BRO1614.md) | Generic type parameter documentation should declare parameter name | SA1621 | on |
| [BRO1615](BRO1615.md) | File should have the XML copyright header (needs `stylebro_file_header_company`) | SA1633-SA1638, SA1640, SA1641 | on |
| [BRO1616](BRO1616.md) | Write the summary's tags consistently on their own lines or on the text's line | (none; Roslynator RCS1253, Meziantou MA0177/MA0211) | on |
| [BRO1617](BRO1617.md) | Write a cref's type arguments in braces | (none; StyleCop's proposed SA1653) | on |
| [BRO1618](BRO1618.md) | Write a C# keyword in documentation as `<see langword="..."/>` | (none; Meziantou MA0154) | on |
| [BRO1619](BRO1619.md) | Put the documentation elements in the standard order | (none) | on |
