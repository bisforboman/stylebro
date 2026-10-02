# Differences from StyleCop

What changes when a project moves from StyleCop.Analyzers with its default settings to StyleBro with its preset. Each
difference is deliberate; this page says why, and how to get StyleCop's behavior back where that's possible.

Teams that changed StyleCop's settings don't need the preset: `stylebro-migrate` writes settings that match theirs
([migrating.md](migrating.md)). Rules StyleBro doesn't cover at all are in [skipped-rules.md](skipped-rules.md).

Kept up to date by tests: every preset setting whose value differs from StyleCop's defaults must be named on this page
(`MigrationTests.EveryPresetDifferenceFromStyleCop_IsDocumented`), and every BRO rule must appear in the rule list
below (`MigrationTests.EveryRule_IsInTheDifferencesPage`).

## Preset choices

The preset follows StyleCop's defaults except here.

| Topic | StyleCop default | StyleBro preset | Why | To get StyleCop's behavior |
|---|---|---|---|---|
| `this.` prefix (SA1101) | Required | Not required (`dotnet_style_qualification_for_field`, `_property`, `_method`, `_event` = `false`; `stylebro-migrate init` writes `dotnet_diagnostic.IDE0009.severity = none`) | Most teams turn SA1101 off (2 of the 3 surveyed); it's also the SDK's default. User decision. | Set the four `dotnet_style_qualification_for_*` keys to `true` and IDE0009 to `warning` in `.editorconfig`. |
| Using placement (SA1200) | Inside the namespace | Outside (`csharp_using_directive_placement = outside_namespace`) | Works with file-scoped namespaces, matches the .NET templates; all three surveyed teams turn SA1200 off. User decision. | `csharp_using_directive_placement = inside_namespace`. |
| Regions between members (SA1124, [BRO1112](rules/BRO1112.md)) | Reported | Off (`dotnet_diagnostic.BRO1112.severity = none`) | Removing every region in an existing codebase is a large one-time change, so it's opt-in. (Regions inside code, SA1123/[BRO1113](rules/BRO1113.md), stay on.) | `dotnet_diagnostic.BRO1112.severity = warning`. |
| Missing documentation (SA1600, SA1601, SA1602, SA1611, SA1615, ...) | Reported | Only overrides and interface implementations, fixed with `<inheritdoc/>` ([BRO1601](rules/BRO1601.md)) | The only automatic fix for other members is placeholder text, which satisfies the rule without documenting anything. User decision. | Not available (keep StyleCop's SA1600 for reporting only, or use the compiler's CS1591). |
| File header (SA1633-SA1641) | Required, XML format (`// <copyright file="X.cs" company="PlaceholderCompany">`) | XML format with [BRO1615](rules/BRO1615.md), but only once `stylebro_file_header_company` is set | A header needs the team's company name and copyright text; a placeholder header everywhere is worse than none. | `stylebro_file_header_company = YourCompany` (and `stylebro_file_header_copyright` if the text differs). For a plain header (`xmlHeader: false`): `file_header_template = ...` and `dotnet_diagnostic.IDE0073.severity = warning`. |

Settings that look different but match StyleCop: `stylebro_private_field_naming = camelCase` (SA1306/SA1309: no
leading underscore), `stylebro_document_*` (stylecop.json's documentationRules defaults), `stylebro_member_*`
(SA1201-SA1204/SA1214 order, checked pairwise), and the SDK settings for spacing, braces, modifiers, type aliases,
parentheses, using order and blank lines. `dotnet_style_require_accessibility_modifiers = for_non_interface_members`
matches SA1400, which doesn't ask for modifiers on interface members (`always` would).

StyleCop rules that are on by default but that nothing enforces under the preset: the **candidate**, **drop** and
**SDK not on yet** entries in [skipped-rules.md](skipped-rules.md) whose facts say "on by default" (for example SA1118,
SA1401, SA1402, SA1649).

## SDK rules that replace StyleCop rules

Where StyleBro relies on an SDK rule instead of shipping its own (its options come from the preset, its severity from
`stylebro-migrate init`), `dotnet format` fixes with the SDK's logic, which isn't
always exactly StyleCop's. Details in [skipped-rules.md](skipped-rules.md#covered-with-known-differences).

- **IDE0047** (for SA1119) also removes the parentheses in `a ?? (b ?? c)`, which SA1119 accepts.
- **IDE2000** (for SA1507) also removes extra blank lines at the start of a file and right before `}`, which StyleCop
  leaves to SA1517 and SA1508.
- **IDE0048** (for SA1407, SA1408) may need a second `dotnet format` run.
- **IDE0036** (for SA1206, SA1207) enforces the SDK's whole modifier order, where StyleCop only checks that the access
  modifier comes first and `static` next. On six real repos it changed nothing StyleCop accepts.
- **IDE0073** (for SA1633 with `xmlHeader: false`) writes a plain header; the XML header is [BRO1615](rules/BRO1615.md).
- **Using sorting** in `dotnet format` happens whenever `dotnet_sort_system_directives_first` is set, even to `false`.

## Rule by rule

Every StyleBro rule reports what its StyleCop rule reports, with the exceptions below; each rule page has the details
under "Compared with StyleCop". Two things differ for every rule:

- **Fixes work under `dotnet format`.** Every StyleBro diagnostic has a fix with Fix All. Several StyleCop fixes don't
  run under `dotnet format` (no Fix All, or they change nothing), apply only part of a rename per run, or produce code
  that breaks another rule or doesn't compile. Those are listed per rule below.
- **Unsafe cases are skipped instead of fixed badly.** Where a fix could change behavior or lose code, StyleBro doesn't
  report the case at all (or, for renames that compile but might break reflection or serialization, reports it and
  leaves the name). StyleCop reports these.

### Reports less than StyleCop

| Rule | Not reported | Why |
|---|---|---|
| [BRO1001](rules/BRO1001.md) (SA1201-SA1204, SA1214) | Types with `#region`/`#if`/`#pragma` between members; types where sorting would change what initializers compute. One diagnostic per type, not per member. | Moving members across directives or reordering dependent initializers changes behavior. |
| [BRO1101](rules/BRO1101.md) (SA1106) | `while (x) ;` and labeled `end: ;` | StyleCop's fix hides a likely bug in the first and breaks the build in the second. |
| [BRO1102](rules/BRO1102.md) (SA1133) | Attribute lists with a comment between the attributes | StyleCop's fix drops the comment. |
| [BRO1103](rules/BRO1103.md) (SA1131) | Comparisons using a type's own `==`/`<` | A user-defined operator may not be symmetric. |
| [BRO1104](rules/BRO1104.md) (SA1129) | `new T()` for type parameters; `new S();` statements | `default(T)` differs for reference types; StyleCop's fix for the statement doesn't compile. |
| [BRO1110](rules/BRO1110.md) (SA1111) | A comment at the end of the last item's line when code follows the `)` (`2 // two` then `); var x = 1;`) | The code would end up behind the comment. |
| [BRO1301](rules/BRO1301.md), [BRO1302](rules/BRO1302.md) (SA1312, SA1313) | `_`/`__` parameters; names whose rename could clash or is inferred elsewhere | Discards; renames must compile. |
| [BRO1303](rules/BRO1303.md) (SA1306, SA1309) | Protected fields; non-private fields (those are BRO1306's); fields of serialized types, attributed fields, names used in strings | Renames must not break serialization or reflection. |
| [BRO1306](rules/BRO1306.md), [BRO1307](rules/BRO1307.md), [BRO1308](rules/BRO1308.md), [BRO1309](rules/BRO1309.md) (SA1303, SA1304, SA1307, SA1311; SA1308; SA1310; SA1300) | Namespaces (SA1300); names that would become keywords or merge digits (`Int32_0`); the same rename guards | Namespace renames move files; the rest must compile and keep working. |
| [BRO1401](rules/BRO1401.md) (SA1413) | Lists with `#if` between the braces | Each target framework's copy needs a different edit. |
| [BRO1508](rules/BRO1508.md), [BRO1509](rules/BRO1509.md) (SA1501, SA1502) | One-line blocks with a comment inside the braces; a `case` block on the line of `switch (x) {`. A local function's body is reported once (BRO1509), not as both SA1501 and SA1502. | The fix would have to move the comment; the `case` block has no line to indent from. |
| [BRO1118](rules/BRO1118.md) (SA1114) | A comment between `(` and the first item | The fix would have to move the comment (StyleCop has no fix). |
| [BRO1513](rules/BRO1513.md) (SA1514) | Documentation right below a `//` comment | The blank line would break BRO1506 (SA1512), and the two fixes would undo each other. |
| [BRO1601](rules/BRO1601.md) (SA1600) | Everything except overrides and interface implementations | See "Missing documentation" above. |
| [BRO1603](rules/BRO1603.md) (SA1629) | Text ending with `?`, `!` or `:` | Already ends a sentence; StyleCop's fix writes `question?.`. |
| [BRO1611](rules/BRO1611.md) (SA1612) | Constructors and operators | Matches StyleCop 1.2, which doesn't check them either. |
| [BRO1612](rules/BRO1612.md), [BRO1614](rules/BRO1614.md) (SA1613, SA1621) | Unnamed `<param>`/`<typeparam>` tags whose (type) parameter isn't certain (two unnamed tags for three parameters, an unnamed tag next to a stale one) | The fix would have to guess the name. |
| [BRO1615](rules/BRO1615.md) (SA1633-SA1638, SA1640, SA1641) | Anything until `stylebro_file_header_company` is set; headers in a `/* */` comment; a broken XML header that already has a `<copyright` tag; a tag sharing its first or last line with other text. One diagnostic per file, on the first line of code when the header is missing. | Placeholder headers help nobody; a broken header needs a person; only whole lines are rewritten. |
| [BRO1122](rules/BRO1122.md) (SA1139) | Casts whose suffixed literal has another value (`(decimal)0.1234567890123456789`, `(decimal)1.50`, a `(float)` of a double literal that rounds differently) | StyleCop's fix changes the number. |
| [BRO1123](rules/BRO1123.md) (SA1141) | `ValueTuple.Create(x, y)` and `new ValueTuple<..>(x, y)` with an argument that would name the element; `new ValueTuple<int, int>()`; creations in expression trees | The names would make BRO1124 change code on a second run; there's no literal for the empty creation (StyleCop's fix writes `()`); expression trees can't contain tuple literals. |
| [BRO1124](rules/BRO1124.md) (SA1142) | `nameof(t.Item1)` | The fix would change the string. |
| [BRO1125](rules/BRO1125.md) (SA1130) | Anonymous methods whose lambda wouldn't bind the same (another overload, no target type for `var`), `ref`/`out` parameters | StyleCop's fix for these changes behavior or doesn't compile. |
| [BRO1126](rules/BRO1126.md) (SA1135) | A qualified name an enclosing namespace would hide | StyleCop's fix would refer to the wrong namespace. |
| [BRO1403](rules/BRO1403.md) (SA1410) | `delegate() { }` that BRO1125 turns into a lambda | One fix per anonymous method; StyleCop reports both rules there. |

### Reports more than StyleCop

| Rule | Also reported | Why |
|---|---|---|
| [BRO1104](rules/BRO1104.md) (SA1129) | Target-typed `new()` for value types | StyleCop 1.1.118 predates it; 1.2 reports it too. |
| [BRO1107](rules/BRO1107.md), [BRO1108](rules/BRO1108.md) (SA1116, SA1117) | Record parameters, primary-constructor parameters and base arguments | StyleCop 1.2.0-beta.556 doesn't check them; they're lists like any other. |
| [BRO1110](rules/BRO1110.md) (SA1111) | A `)` on its own line after target-typed `new(...)` and `: this(...)`/`: base(...)` arguments | StyleCop (1.1.118 and 1.2) doesn't check these lists; they're argument lists like any other. |
| [BRO1306](rules/BRO1306.md) (SA1303, SA1311) | Constants and `static readonly` fields starting with `_` | StyleCop leaves them to SA1309; StyleBro renames to the full correct name in one pass. |
| [BRO1123](rules/BRO1123.md) (SA1141) | Tuple types in locals, arrays, nullables, `typeof` and type arguments of calls | StyleCop only checks declarations and creations. |
| [BRO1125](rules/BRO1125.md) (SA1130) | Calls like `list.ForEach(delegate (int item) { ... })` | StyleCop's overload check misses them; StyleBro binds the lambda and finds the same method. |
| [BRO1505](rules/BRO1505.md) (SA1516) | A member whose `///` directly follows the previous member, and file-scoped namespace declarations | StyleCop misses the first when it parses docs, and 1.1.118 misses the second (1.2 reports it). |
| [BRO1002](rules/BRO1002.md) (SA1005) | Nothing extra; StyleCop 1.1.118 reported `//  two spaces`, 1.2 and StyleBro don't | |

### Same reports, different fix

| Rule | StyleCop's fix | StyleBro's fix |
|---|---|---|
| [BRO1002](rules/BRO1002.md) (SA1005) | `// ` with a trailing space for an empty comment | `//` |
| [BRO1108](rules/BRO1108.md) (SA1117) | None that works | Each item on its own line |
| [BRO1301](rules/BRO1301.md)-[BRO1309](rules/BRO1309.md) (naming) | Lowers/removes only the first letter or prefix, can't fix `_name`, under `dotnet format` applies part of the renames per run or nothing; `MAX_VALUE` -> `MAXVALUE`, `m_Upper` -> `Upper` | The complete correct name in one pass (`MaxValue`), with overrides, implementations, named arguments and docs |
| [BRO1401](rules/BRO1401.md) (SA1413) | `"beta",}` (no space), nested lists need a second run | `"beta", }`, nested lists in one pass |
| [BRO1505](rules/BRO1505.md) (SA1516) | Misses the blank line above a `///` comment; splits two members on a line without indentation | Inserts above the doc comment; indents |
| [BRO1508](rules/BRO1508.md), [BRO1509](rules/BRO1509.md) (SA1501, SA1502) | Writes `{` at column 1 in nested blocks, leaves accessors and nested declarations side by side, moves `while` of `do ... while` to its own line, writes CRLF into `\n` files | Indents from `.editorconfig`, one item per line, adds the blank lines BRO1505 wants, keeps `} while (x);` and the file's line endings, respects `csharp_new_line_before_open_brace`/`_else`/`_catch`/`_finally` |
| [BRO1510](rules/BRO1510.md) (SA1504) | Under `dotnet format`, collapses or expands the accessors of every property in the project the same way, whichever the first diagnostic offers; deletes comments in a body it collapses | Collapses one-statement bodies or expands, per property; never collapses a body with a comment; adds the blank line BRO1505 wants |
| [BRO1604](rules/BRO1604.md) (SA1623) | `a value indicating whether whether ...` | Keeps the existing "whether" |
| [BRO1606](rules/BRO1606.md), [BRO1607](rules/BRO1607.md) (SA1642, SA1643) | `class.Opens a connection` (no space) | `class. Opens a connection` |
| [BRO1114](rules/BRO1114.md) (SA1132) | Keeps the attributes on the first field only, so the others lose them (`[Obsolete]` disappears from the second field) | Copies the attributes and the documentation comment to every field; blank line between event fields |
| [BRO1115](rules/BRO1115.md) (SA1125) | None | `int?` for `Nullable<int>`, nested ones in one pass |
| [BRO1402](rules/BRO1402.md) (SA1411) | `[Obsolete ]` for `[Obsolete( )]` | `[Obsolete]` |
| [BRO1118](rules/BRO1118.md), [BRO1119](rules/BRO1119.md) (SA1114, SA1115) | None | Removes the blank lines |
| [BRO1120](rules/BRO1120.md) (SA1120) | Removes only the reported empty comment; the next one at the same end is reported on the next run | Removes every empty comment at that end of the group |
| [BRO1121](rules/BRO1121.md) (SA1136) | For an enum on one line, leaves `{ A,` and `C }` on the brace lines | Expands the enum like BRO1509 |
| [BRO1610](rules/BRO1610.md), [BRO1611](rules/BRO1611.md) (SA1627, SA1612) | None | Removes the empty `<remarks>`; renames, removes and reorders `<param>` tags |
| [BRO1612](rules/BRO1612.md), [BRO1613](rules/BRO1613.md), [BRO1614](rules/BRO1614.md) (SA1613, SA1620, SA1621) | None | Names unnamed tags when certain; renames, removes and reorders `<typeparam>` tags |
| [BRO1112](rules/BRO1112.md) (SA1124) | Removes a region between switch-expression arms with the blank lines around it | Keeps one blank line where it was |
| [BRO1615](rules/BRO1615.md) (SA1633-SA1641) | Deletes a plain comment header (license text, notes) when it writes the XML header | Keeps any other comment below the new header; otherwise the same headers |
| [BRO1123](rules/BRO1123.md), [BRO1124](rules/BRO1124.md) (SA1141, SA1142) | None under `dotnet format` (SA1141's has no Fix All, SA1142's throws) | Tuple syntax and element names |
| [BRO1125](rules/BRO1125.md) (SA1130) | Leaves two spaces after `=`, pulls a body on its own line up behind `=>` | Keeps the layout |

### Same as StyleCop

Reports and fixes match StyleCop (checked with `scripts/stylecop-survey/Compare-WithStyleCop.ps1`):
[BRO1003](rules/BRO1003.md), [BRO1004](rules/BRO1004.md), [BRO1005](rules/BRO1005.md), [BRO1116](rules/BRO1116.md), [BRO1117](rules/BRO1117.md),
[BRO1511](rules/BRO1511.md), [BRO1512](rules/BRO1512.md), [BRO1105](rules/BRO1105.md), [BRO1106](rules/BRO1106.md), [BRO1109](rules/BRO1109.md), [BRO1111](rules/BRO1111.md),
[BRO1113](rules/BRO1113.md), [BRO1304](rules/BRO1304.md), [BRO1305](rules/BRO1305.md), [BRO1501](rules/BRO1501.md),
[BRO1502](rules/BRO1502.md), [BRO1503](rules/BRO1503.md), [BRO1504](rules/BRO1504.md), [BRO1506](rules/BRO1506.md),
[BRO1507](rules/BRO1507.md), [BRO1602](rules/BRO1602.md), [BRO1605](rules/BRO1605.md), [BRO1608](rules/BRO1608.md),
[BRO1609](rules/BRO1609.md). (BRO1304, BRO1305 and BRO1309 have working fixes under `dotnet format`; StyleCop's
change nothing there.)
