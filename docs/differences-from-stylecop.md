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
| `this.` prefix (SA1101) | Required | Not required (`dotnet_style_qualification_for_field`, `_property`, `_method`, `_event` = `false`; `stylebro-migrate init` writes `dotnet_diagnostic.IDE0009.severity = none`) | Most teams turn SA1101 off (2 of the 3 surveyed); it's also the SDK's default. User decision. With these keys `false`, [BRO1131](rules/BRO1131.md) (SA1100) removes `base.` instead of writing `this.`. | Set the four `dotnet_style_qualification_for_*` keys to `true` and IDE0009 to `warning` in `.editorconfig`. |
| Using placement (SA1200) | Inside the namespace | Outside (`csharp_using_directive_placement = outside_namespace`) | Works with file-scoped namespaces, matches the .NET templates; all three surveyed teams turn SA1200 off. User decision. | `csharp_using_directive_placement = inside_namespace`. |
| Regions between members (SA1124, [BRO1112](rules/BRO1112.md)) | Reported | Off (`dotnet_diagnostic.BRO1112.severity = none`) | Removing every region in an existing codebase is a large one-time change, so it's opt-in. (Regions inside code, SA1123/[BRO1113](rules/BRO1113.md), stay on.) | `dotnet_diagnostic.BRO1112.severity = warning`. |
| Namespace names (SA1300, [BRO1312](rules/BRO1312.md)) | Reported | Off (`dotnet_diagnostic.BRO1312.severity = none`) | Renaming a namespace renames every type in it: a breaking change for other code and for stored type names (`$type`, `Type.GetType`). In the surveyed repositories every lower-case namespace part was deliberate (brand names like `iText`, `iOS`; culture codes). User decision; findings in [proposals/namespace-names.md](proposals/namespace-names.md). | `dotnet_diagnostic.BRO1312.severity = warning`. |
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
| [BRO1001](rules/BRO1001.md) (SA1201-SA1204, SA1214) | Members out of order across regions (each region is sorted on its own); types with `#if`/`#else`/`#endif` between members; types where sorting would change what initializers compute, a struct's (or `[StructLayout]` type's) field layout, or the order a serializer writes attributed members in. One diagnostic per type, not per member. | Moving members across directives or reordering dependent initializers changes behavior. |
| [BRO1514](rules/BRO1514.md), [BRO1515](rules/BRO1515.md), [BRO1516](rules/BRO1516.md) (SA1503, SA1519, SA1520) | A comment or directive where the braces would go, `#if` in the owning statement, a multi-line string in the statement, other code after it on its line | The fix can't place the braces without moving the comment or reindenting string contents; StyleCop's formatter-based fix does. |
| [BRO1311](rules/BRO1311.md) (SA1316) | Names in tuple literals and inferred names (StyleCop's `includeInferredTupleElementNames`, off by default) | Not tuple types; renaming an inferred name means renaming the variable it comes from. |
| [BRO1132](rules/BRO1132.md) (SA1108) | A statement header spanning several lines (`if (a` + `&& b) // c`); a comment before a single-line block (`if (x) // c` + `{ y(); }`), a comment spanning lines, a directive between the header and `{` | After a split condition the comment usually explains its last line, not the block; moved into the block it would mislead (user decision). The fix needs a line of its own inside the block for the comment, and moves only one-line comments. |
| [BRO1310](rules/BRO1310.md) (SA1305) | Constant, `static readonly`, public and internal fields; names where another name in the member would get the same new name (`xItem`, `yItem`) | Pascal fields are [BRO1306](rules/BRO1306.md)'s, which removes the prefix; the rename must not collide. |
| [BRO1405](rules/BRO1405.md) (SA1119) | Parentheses whose removal changes how the code parses (`F(a < b, (c > (d + 1)))` would become a generic call) | StyleCop's fix doesn't compile there. Its fix also leaves the spaces of `( b )` behind; StyleBro's removes them. |
| [BRO1517](rules/BRO1517.md), [BRO1518](rules/BRO1518.md), [BRO1519](rules/BRO1519.md) (SA1507, SA1508, SA1513) | Places another StyleBro blank-line rule fixes when it's on: after `{` (BRO1503), below a comment (BRO1506), a comment, documentation or member after `}` (BRO1504, BRO1513, BRO1505); also a `}` with code or a comment after it on its line | Two fixes adding or removing the same blank line would conflict under `dotnet format`. |
| [BRO1101](rules/BRO1101.md) (SA1106) | `while (x) ;` and labeled `end: ;` | StyleCop's fix hides a likely bug in the first and breaks the build in the second. |
| [BRO1102](rules/BRO1102.md) (SA1133) | Attribute lists with a comment between the attributes | StyleCop's fix drops the comment. |
| [BRO1103](rules/BRO1103.md) (SA1131) | Comparisons using a type's own `==`/`<` | A user-defined operator may not be symmetric. |
| [BRO1131](rules/BRO1131.md) (SA1100) | `base.M()` for a virtual M in a type that isn't sealed | `this.M()` would call a derived type's override instead: a behavior change. |
| [BRO1104](rules/BRO1104.md) (SA1129) | `new T()` for type parameters; `new S();` statements | `default(T)` differs for reference types; StyleCop's fix for the statement doesn't compile. |
| [BRO1110](rules/BRO1110.md) (SA1111) | A comment at the end of the last item's line when code follows the `)` (`2 // two` then `); var x = 1;`) | The code would end up behind the comment. |
| [BRO1301](rules/BRO1301.md), [BRO1302](rules/BRO1302.md) (SA1312, SA1313) | `_`/`__` parameters; names whose rename could clash or is inferred elsewhere; constructor parameters named exactly like a property or field of their type | Discards; renames must compile; serializers bind constructor parameters to members by name (CsvHelper case-sensitively). |
| [BRO1303](rules/BRO1303.md) (SA1306, SA1309) | Non-private, non-protected fields (those are BRO1306's); fields of serialized types, attributed fields, names used in strings | Renames must not break serialization or reflection. |
| [BRO1306](rules/BRO1306.md), [BRO1307](rules/BRO1307.md), [BRO1308](rules/BRO1308.md), [BRO1309](rules/BRO1309.md) (SA1303, SA1304, SA1307, SA1311; SA1308; SA1310; SA1300) | Namespaces (SA1300's, they are [BRO1312](rules/BRO1312.md)'s); names that would become keywords or merge digits (`Int32_0`); the same rename guards | Renames must compile and keep working. |
| [BRO1312](rules/BRO1312.md) (SA1300, namespaces) | Off by default. Not reported: the project's root namespace or a part of it, a namespace another assembly also declares, one declared in generated code, a new name that already exists (`taken` next to `Taken`) | Resource names follow the root namespace, not the declaration; the other assembly and the generator keep the old name; merging two namespaces can make type names collide. |
| [BRO1401](rules/BRO1401.md) (SA1413) | Lists with `#if` between the braces | Each target framework's copy needs a different edit. |
| [BRO1508](rules/BRO1508.md), [BRO1509](rules/BRO1509.md) (SA1501, SA1502) | One-line blocks with a comment inside the braces; a `case` block on the line of `switch (x) {`. A local function's body is reported once (BRO1509), not as both SA1501 and SA1502. | The fix would have to move the comment; the `case` block has no line to indent from. |
| [BRO1118](rules/BRO1118.md) (SA1114) | A comment between `(` and the first item | The fix would have to move the comment (StyleCop has no fix). |
| [BRO1513](rules/BRO1513.md) (SA1514) | Documentation right below a `//` comment | The blank line would break BRO1506 (SA1512), and the two fixes would undo each other. |
| [BRO1504](rules/BRO1504.md) (SA1515) | A comment right after a collection expression's `[` | It starts the list like a comment after `{`; StyleCop fixed this after 1.2.0-beta.556 ([#3766](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3766)). |
| [BRO1501](rules/BRO1501.md) (SA1509) | The brace of an entry in an initializer (`{ "ssh", 22 }` after a blank line in a dictionary initializer) | The blank line groups the entries; StyleCop's fix removes it (StyleCop bug [#2832](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2832), open; user decision 2026-10-04). |
| [BRO1504](rules/BRO1504.md) (SA1515) | A comment right after `=>` (switch expression arm, lambda, expression body) | It starts what comes after the arrow; StyleCop's fix puts a blank line between them (StyleCop bug [#3392](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3392), open; user decision 2026-10-04). |
| [BRO1504](rules/BRO1504.md) (SA1515) | A comment whose text starts with a prefix in `stylebro_comment_blank_line_exempt_prefixes` (default none; e.g. `ReSharper, @formatter`) | Tool markers belong to the line below them (StyleCop issues [#3546](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3546), [#1490](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1490)); with the default, the same as StyleCop. |
| [BRO1310](rules/BRO1310.md) (SA1305) | Parameters of `extern`, `[DllImport]` and `[LibraryImport]` methods | They keep the native API's names, like BRO1309 leaves the methods alone (StyleCop bug [#2859](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2859), open). |
| [BRO1002](rules/BRO1002.md) (SA1005) | `dotnet new` template markers `//-:` and `//+:` | A space breaks them (StyleCop bug [#2689](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2689), open). |
| [BRO1510](rules/BRO1510.md) (SA1504) | One-line accessors where one has an attribute on its own line above it | The attribute isn't part of the accessor's layout; StyleCop counts its line ([#3434](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3434)). |
| [BRO1601](rules/BRO1601.md) (SA1600) | Everything except overrides and implicit interface implementations | See "Missing documentation" above; explicit implementations like StyleCop's `master`. |
| [BRO1603](rules/BRO1603.md) (SA1629) | Text ending with `?`, `!` or `:`; a period followed by a closing quote or bracket (`"done."`) | Already ends a sentence; StyleCop's fix writes `question?.` and `"done.".`. |
| [BRO1603](rules/BRO1603.md) (SA1629) | Text ending with `?`, `!` or `:`; a period followed by a closing quote or bracket (`"done."`); a trailing `<c>"... ."</c>` | Already ends a sentence; StyleCop's fix writes `question?.`, `"done.".` and `</c>.` (StyleCop bug [#2784](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2784), open). |
| [BRO1606](rules/BRO1606.md) (SA1642) | A summary whose first `<para>` starts with the standard sentence | StyleCop fixed this after 1.2.0-beta.556; beta.556's fix writes the sentence twice. |
| [BRO1611](rules/BRO1611.md) (SA1612) | Constructors and operators | Matches StyleCop 1.2, which doesn't check them either. |
| [BRO1612](rules/BRO1612.md), [BRO1614](rules/BRO1614.md) (SA1613, SA1621) | Unnamed `<param>`/`<typeparam>` tags whose (type) parameter isn't certain (two unnamed tags for three parameters, an unnamed tag next to a stale one) | The fix would have to guess the name. |
| [BRO1615](rules/BRO1615.md) (SA1633-SA1638, SA1640, SA1641) | Anything until `stylebro_file_header_company` is set; headers in a `/* */` comment; a broken XML header that already has a `<copyright` tag; a tag sharing its first or last line with other text. One diagnostic per file, on the first line of code when the header is missing. | Placeholder headers help nobody; a broken header needs a person; only whole lines are rewritten. |
| [BRO1122](rules/BRO1122.md) (SA1139) | Casts whose suffixed literal has another value (`(decimal)0.1234567890123456789`, `(decimal)1.50`, a `(float)` of a double literal that rounds differently) | StyleCop's fix changes the number. |
| [BRO1123](rules/BRO1123.md) (SA1141) | `ValueTuple.Create(x, y)` and `new ValueTuple<..>(x, y)` with an argument that would name the element; `new ValueTuple<int, int>()`; creations in expression trees | The names would make BRO1124 change code on a second run; there's no literal for the empty creation (StyleCop's fix writes `()`); expression trees can't contain tuple literals. |
| [BRO1124](rules/BRO1124.md) (SA1142) | `nameof(t.Item1)` | The fix would change the string. |
| [BRO1125](rules/BRO1125.md) (SA1130) | Anonymous methods whose lambda wouldn't bind the same (another overload, no target type for `var`), `ref`/`out` parameters | StyleCop's fix for these changes behavior or doesn't compile. |
| [BRO1126](rules/BRO1126.md) (SA1135) | A qualified name an enclosing namespace would hide; a type argument that starts with an alias | StyleCop's fix would refer to the wrong namespace; an alias at the start of the name is fine for StyleCop too (StyleCop bug [#3884](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3884), open). |
| [BRO1127](rules/BRO1127.md), [BRO1128](rules/BRO1128.md) (SA1102, SA1103) | Blank lines with a comment line between clauses; a query whose clauses share a line across a comment | The fix would have to move the comment, or couldn't satisfy the rule. |
| [BRO1403](rules/BRO1403.md) (SA1410) | `delegate() { }` that BRO1125 turns into a lambda | One fix per anonymous method; StyleCop reports both rules there. |

### Reports more than StyleCop

| Rule | Also reported | Why |
|---|---|---|
| [BRO1310](rules/BRO1310.md) (SA1305) | A prefix after a leading underscore (`_iCount`) | StyleCop reports it once SA1309 removed the underscore; StyleBro does both in one rename. |
| [BRO1407](rules/BRO1407.md) (SA1408) | `and`/`or` mixed in a pattern (`v is > 1 and < 5 or 10`) | StyleCop's current source checks patterns too; the released 1.2.0-beta.556 predates it. |
| [BRO1104](rules/BRO1104.md) (SA1129) | Target-typed `new()` for value types | StyleCop 1.1.118 predates it; 1.2 reports it too. |
| [BRO1107](rules/BRO1107.md), [BRO1108](rules/BRO1108.md) (SA1116, SA1117) | Record parameters, primary-constructor parameters and base arguments | StyleCop 1.2.0-beta.556 doesn't check them; they're lists like any other. |
| [BRO1110](rules/BRO1110.md) (SA1111) | A `)` on its own line after target-typed `new(...)` and `: this(...)`/`: base(...)` arguments | StyleCop (1.1.118 and 1.2) doesn't check these lists; they're argument lists like any other. |
| [BRO1306](rules/BRO1306.md) (SA1303, SA1311) | Constants and `static readonly` fields starting with `_` | StyleCop leaves them to SA1309; StyleBro renames to the full correct name in one pass. |
| [BRO1123](rules/BRO1123.md) (SA1141) | Tuple types in locals, arrays, nullables, `typeof` and type arguments of calls | StyleCop only checks declarations and creations. |
| [BRO1125](rules/BRO1125.md) (SA1130) | Calls like `list.ForEach(delegate (int item) { ... })` | StyleCop's overload check misses them; StyleBro binds the lambda and finds the same method. |
| [BRO1128](rules/BRO1128.md) (SA1103) | A mixed query that also has SA1104/SA1105 findings | StyleCop leaves the rest of the query to a second run. |
| [BRO1510](rules/BRO1510.md) (SA1504) | A one-line accessor with an attribute above it next to a multi-line accessor | StyleCop counts the attribute line, so it sees two multi-line accessors ([#3434](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3434)). |
| [BRO1505](rules/BRO1505.md) (SA1516) | A member whose `///` directly follows the previous member, and file-scoped namespace declarations | StyleCop misses the first when it parses docs, and 1.1.118 misses the second (1.2 reports it). |
| [BRO1002](rules/BRO1002.md) (SA1005) | Nothing extra; StyleCop 1.1.118 reported `//  two spaces`, 1.2 and StyleBro don't | |

### Same reports, different fix

| Rule | StyleCop's fix | StyleBro's fix |
|---|---|---|
| [BRO1311](rules/BRO1311.md) (SA1316) | Renames the declaration only: every use (`t.count`) and override then breaks the build | Renames every tuple type element with the name, the literals converted to them and every use, in the whole solution; a rename that wouldn't compile is skipped and the warning stays |
| [BRO1002](rules/BRO1002.md) (SA1005) | `// ` with a trailing space for an empty comment | `//` |
| [BRO1108](rules/BRO1108.md) (SA1117) | None that works | Each item on its own line |
| [BRO1132](rules/BRO1132.md) (SA1108) | None | Moves the comment into the block, on its own line right after `{` |
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
| [BRO1128](rules/BRO1128.md) (SA1103) | Joins the query on one line or splits it, whichever action comes first | Each clause on its own line |
| [BRO1107](rules/BRO1107.md), [BRO1108](rules/BRO1108.md) (SA1116, SA1117) | Leaves a moved lambda's block body at its old indentation (StyleCop bugs [#1620](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1620), [#3183](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3183), open) | Moves the body with the lambda |

### Same as StyleCop

Reports and fixes match StyleCop (checked with `scripts/stylecop-survey/Compare-WithStyleCop.ps1`):
[BRO1003](rules/BRO1003.md), [BRO1004](rules/BRO1004.md), [BRO1005](rules/BRO1005.md), [BRO1006](rules/BRO1006.md), [BRO1007](rules/BRO1007.md), [BRO1404](rules/BRO1404.md), [BRO1406](rules/BRO1406.md), [BRO1116](rules/BRO1116.md), [BRO1117](rules/BRO1117.md),
[BRO1511](rules/BRO1511.md), [BRO1512](rules/BRO1512.md), [BRO1105](rules/BRO1105.md), [BRO1106](rules/BRO1106.md), [BRO1109](rules/BRO1109.md), [BRO1111](rules/BRO1111.md),
[BRO1113](rules/BRO1113.md), [BRO1304](rules/BRO1304.md), [BRO1305](rules/BRO1305.md), [BRO1501](rules/BRO1501.md),
[BRO1502](rules/BRO1502.md), [BRO1503](rules/BRO1503.md), [BRO1506](rules/BRO1506.md),
[BRO1507](rules/BRO1507.md), [BRO1602](rules/BRO1602.md), [BRO1605](rules/BRO1605.md), [BRO1608](rules/BRO1608.md),
[BRO1609](rules/BRO1609.md), [BRO1129](rules/BRO1129.md), [BRO1130](rules/BRO1130.md). (BRO1304, BRO1305 and BRO1309 have working fixes under `dotnet format`; StyleCop's
change nothing there.)

### Compared with StyleCop's unreleased `master`

The tables above compare with the last release, 1.2.0-beta.556. StyleCop's `master` has changed some of these rules
since (not released; see [decisions](decisions.md), 2026-10-04). Where `master` fixed a gap, StyleBro follows it:

| Rule | Follows `master` | beta.556 |
|---|---|---|
| [BRO1104](rules/BRO1104.md) (SA1129) | `new nint()` -> `default(nint)` (also on .NET 7+: the fix must agree across a multi-targeted project's frameworks) | Always `nint.Zero` (CS0117 on older runtimes); `master`: `nint.Zero` on C# 11 + .NET 7+ |
| [BRO1604](rules/BRO1604.md), [BRO1605](rules/BRO1605.md) (SA1623, SA1624) | `init` accessors: `Gets or initializes` or `Gets`, `Initializes`; `Gets or initializes` is a wrong prefix elsewhere | Ignores `init`; its fix writes `Gets or sets or initializes` |
| [BRO1611](rules/BRO1611.md) (SA1612) | `<param>` tags of primary constructors (classes, structs, records) | Not checked |
| [BRO1401](rules/BRO1401.md) (SA1413) | Multi-line property patterns | Not checked |
| [BRO1505](rules/BRO1505.md) (SA1516) | No blank line needed between two single-line properties | Wants one |
| [BRO1601](rules/BRO1601.md) (SA1600) | Explicit interface implementations not reported | Reported |

Where `master` changed behavior StyleBro keeps (the owner's decision):

| Rule | `master` | StyleBro (like beta.556) |
|---|---|---|
| [BRO1505](rules/BRO1505.md) (SA1516) | Two fields need a blank line when the second spans several lines | Only when the first does |
| [BRO1606](rules/BRO1606.md) (SA1642) | Any summary that starts with `<para>` is accepted, text not checked | Checks the paragraph's text |
| [BRO1311](rules/BRO1311.md) (SA1316) | Names in an override's or implementation's signature not reported | Reported, renamed together with the base |

## Rules beyond StyleCop

Rules StyleCop doesn't have. The preset turns them on (`dotnet_diagnostic.BRO1133.severity`,
`dotnet_diagnostic.BRO1520.severity`, `dotnet_diagnostic.BRO1521.severity` and `dotnet_diagnostic.BRO1522.severity` =
Rules StyleCop doesn't have. The preset turns them on (`dotnet_diagnostic.BRO1520.severity`,
`dotnet_diagnostic.BRO1521.severity`, `dotnet_diagnostic.BRO1522.severity` and `dotnet_diagnostic.BRO1134.severity` =
`dotnet_diagnostic.BRO1521.severity`, `dotnet_diagnostic.BRO1522.severity` and `dotnet_diagnostic.BRO1523.severity` =
`dotnet_diagnostic.BRO1521.severity`, `dotnet_diagnostic.BRO1522.severity` and `dotnet_diagnostic.BRO1135.severity` =
`dotnet_diagnostic.BRO1521.severity`, `dotnet_diagnostic.BRO1522.severity` and `dotnet_diagnostic.BRO1525.severity` =
`dotnet_diagnostic.BRO1521.severity`, `dotnet_diagnostic.BRO1522.severity` and `dotnet_diagnostic.BRO1524.severity` =
`dotnet_diagnostic.BRO1521.severity`, `dotnet_diagnostic.BRO1522.severity` and `dotnet_diagnostic.BRO1616.severity` =
`dotnet_diagnostic.BRO1617.severity`, `dotnet_diagnostic.BRO1618.severity` and `dotnet_diagnostic.BRO1619.severity` =
`warning`); `stylebro-migrate` writes
them as `none`, so a StyleCop-clean repository doesn't change when it migrates. Turn them on in `.editorconfig` to use
them.

| Rule | What it checks | From |
|---|---|---|
| [BRO1133](rules/BRO1133.md) | Null checks as `is null`/`is not null` (`stylebro_null_check_style = pattern_matching`, the default) or `== null`/`!= null` (`equality_operator`) | Roslynator RCS1248 |
| [BRO1520](rules/BRO1520.md) | Binary operators and `?`/`:` at the beginning of the line when an expression wraps (`dotnet_style_operator_placement_when_wrapping`) | Roslynator RCS0027/RCS0028 |
| [BRO1521](rules/BRO1521.md) | `=>` of expression bodies and switch arms at the end of the line (`stylebro_arrow_placement_when_wrapping`) | Roslynator RCS0032 |
| [BRO1522](rules/BRO1522.md) | `=` of assignments and initializers at the end of the line (`stylebro_equals_placement_when_wrapping`) | Roslynator RCS0052 |
| [BRO1134](rules/BRO1134.md) | A comment between a type's, namespace's, member's, accessor's or local function's header and its `{` moves into the body, like BRO1132 (SA1108) for statements | StyleCop issue [#605](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/605) (proposed, never implemented) |
| [BRO1523](rules/BRO1523.md) | In a call chain split over several lines, every call after the first line starts its own line | Roslynator RCS0054 |
| [BRO1135](rules/BRO1135.md) | Integer literal suffixes in upper case (`1L`, `2U`, `3UL`); real suffixes (`f`, `d`, `m`) aren't checked | Sonar S818, StyleCop issue #1563 |
| [BRO1525](rules/BRO1525.md) | No blank line between an attribute list and its element or the element's next attribute list | StyleCop's proposed SA1521 ([#738](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/738), never implemented) |
| [BRO1524](rules/BRO1524.md) | A split conditional expression has the condition, `? a` and `: b` on their own lines (`?`/`:` side from `dotnet_style_operator_placement_when_wrapping`); chains aren't checked | StyleCop issue [#651](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/651) (proposed, never implemented) |
| [BRO1616](rules/BRO1616.md) | `<summary>` tags on lines of their own (`stylebro_summary_layout = multi_line`), or the summary on one line when its text is one line and fits (`single_line_when_fits`) | Roslynator RCS1253, Meziantou MA0177/MA0211 |
| [BRO1313](rules/BRO1313.md) | Off by default. A parameter of an override or interface implementation has the base member's parameter name (named arguments updated) | StyleCop's proposed SA1315 ([#1949](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1949), never implemented); SDK CA1725 (no fix) |
| [BRO1314](rules/BRO1314.md) | Off by default. Methods returning `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` or `IAsyncEnumerable<T>` end in `Async`; tests, controllers, hubs, event handlers and attributed methods skipped | Roslynator RCS1046, Meziantou MA0137 |
| [BRO1617](rules/BRO1617.md) | A cref's type arguments in braces: `cref="List{T}"`, not `cref="List&lt;T&gt;"` | StyleCop's proposed SA1653 ([#758](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/758), never implemented) |
| [BRO1618](rules/BRO1618.md) | A C# keyword alone in `<c>` (`<c>null</c>`) is written `<see langword="null"/>` | Meziantou MA0154 |
| [BRO1619](rules/BRO1619.md) | Top-level documentation elements in the order summary, typeparam, param, returns, value, exception, remarks, example, seealso | (none; Roslynator RCS1232 orders `<param>` tags only) |

## StyleCop's open bugs

Every open StyleCop.Analyzers issue on a rule StyleBro replaces was read (2026-10-04: 430 open issues, 97 name such a
rule and aren't feature requests), and each one that describes a behavior StyleBro could share was run against StyleBro
with a throwaway test: 50 open issues and 4 closed as not planned. The others were already covered in
[beyond-stylecop.md](beyond-stylecop.md) (21) or aren't about behavior StyleBro could share (23: documentation wording,
configuration questions, the IDE, missing-documentation rules StyleBro drops).

| Result | Issues |
|---|---|
| StyleBro doesn't have the bug | 34 (30 open, 4 not planned) |
| StyleBro had it too | 8: 7 fixed on 2026-10-04, 1 kept like StyleCop by the owner's decision ([decisions.md](decisions.md)) |
| Not applicable, or StyleBro is the same or different by design | 12 |

| Issue | Rule | Bug | StyleBro |
|---|---|---|---|
| [#1488](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1488) | SA1519 -> BRO1515 | Braceless child under a multi-line `if (a &&\n b)` header not reported | Same or different by design: Same as SA1519 by default; `csharp_prefer_braces = when_multiline` reports and fixes it |
| [#1588](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1588) | SA1201 -> BRO1001 | Fix moves a method into `#if DEBUG` | Not shared: Not reported (conditional directives are skipped) |
| [#1620](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1620) | SA1116 -> BRO1107 | Fix doesn't reindent a multi-line lambda argument | Shared, fixed: the lambda body moves with the lambda |
| [#1956](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1956), [#3358](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3358) | SA1102 -> BRO1127 | Comment between query clauses reported, fix deletes it | Not shared: Gaps with comments skipped; nothing deleted |
| [#2081](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2081) | SA1626 -> BRO1602 | `//` line inside a `///` block: fix strips slashes from doc lines | Not shared: Not reported, unchanged |
| [#2112](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2112) | SA1203 -> BRO1001 | Fix across `#region`s produces broken regions | Not shared: Regions are fences; not reported |
| [#2183](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2183) | SA1519 -> BRO1515 | Wrong indentation wrapping consecutive `using`s | Not shared: With `stylebro_allow_consecutive_usings = false`, correctly nested and indented |
| [#2696](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2696) | SA1642 -> BRO1606 | Fix output for empty `<summary></summary>` | Not shared: Becomes `<summary>Initializes a new instance of the <see cref="C"/> class.</summary>`; `<summary/>` not reported |
| [#2832](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2832) | SA1509 -> BRO1501 | Blank line before `{ k, v }` in a dictionary initializer reported | Shared, fixed: an initializer's entries keep their grouping blank line (owner's decision, 2026-10-04) |
| [#3539](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3539) | SA1509 -> BRO1501 | Blank line before a standalone `{ }` block reported | Same or different by design: matched on purpose (a standalone block after `;` is reported, like SA1509) |
| [#2880](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2880) | SA1100 -> BRO1131 | `base.Get<T>(x)` from an override of `Get(x)` | Not shared: Rewritten to `this.Get<MyExports>(x)`, same method (maintainer agreed it's correct) |
| [#3379](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3379) | SA1100 -> BRO1131 | `base.M(other)` in the override of generic virtual `M<T>` reported | Not shared: Not reported |
| [#2890](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2890) | SA1202/SA1203 -> BRO1001 | SA1202 and SA1203 want opposite orders | Not shared: One sort key; fix converges, compiles |
| [#2939](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2939) | SA1309 -> BRO1303 | Rename `_documentIndex` clashes with `out int documentIndex` (CS0844) | Not shared: References qualified with `this.`; compiles |
| [#2967](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2967) | SA1642 -> BRO1606 | `cref="AuthorizationPolicy{XXX}"` not reported | Not shared: Reported and fixed to `{TIDENT}` (stricter than StyleCop, like classic) |
| [#2984](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2984) | SA1642 -> BRO1606 | `<include>` docs not expanded | Not shared: `<include>` members not reported |
| [#3122](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3122) | SA1627 -> BRO1610 | Empty remarks reported for `<include>` | Not shared: Not reported |
| [#3217](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3217) | SA1139 -> BRO1122 | NullReferenceException on `(ulong)14` in an attribute; FP on `unchecked((int)0xFFFFFF0F)` | Not shared: No crash; attribute case fixed to `14UL`; the `unchecked` cast not reported |
| [#3284](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3284) | SA1114 -> BRO1118 | FP on an argument list with `#if` | Not shared: Not reported |
| [#2917](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2917) | SA1114 -> BRO1118 | Comment line after `(` reported | Not shared: Not reported |
| [#3392](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3392) | SA1515 -> BRO1504 | Comment right after `=>` in a switch-expression arm reported | Shared, fixed: `=>` counts like `{` (owner's decision, 2026-10-04) |
| [#3568](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3568) | SA1513 -> BRO1519 | Blank line demanded between a case block's `}` and the next `case` | Shared, kept: same as SA1513 (owner's decision, 2026-10-04) |
| [#3581](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3581) | SA1503 -> BRO1514 | Fix ignores a custom indentation size | Not shared: `indent_size = 1` honored |
| [#3882](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3882), [#3916](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3916) | SA1135 -> BRO1126 | AD0001 on `using X = string;` / tuple aliases | Not shared: No crash, not reported |
| [#3884](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3884) | SA1135 -> BRO1126 | `using T1 = Tasks.Task;` allowed but `using T2 = System.ValueTuple<Tasks.Task, int>;` reported | Shared, fixed: a type argument starting with an alias is kept |
| [#3921](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3921) | SA1642 -> BRO1606 | Record struct expected to say "class" | Same or different by design: Records are skipped (documented); plain struct fixed to "struct" correctly |
| [#3953](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3953) | SA1413 -> BRO1401 | Nested initializers need 3 `dotnet format` runs | Not shared: One pass |
| [#3948](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3948) | SA1516 -> BRO1505 | Blank line demanded between a property and its backing field | Same or different by design: By design in both (maintainer: fields first) |
| [#3159](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3159) | SA1516 -> BRO1505 | Usings: blank-line groups handled inconsistently | Not shared: Using+using exempt; not reported |
| [#3932](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3932) (= #3849) | SA1514 -> BRO1513 | Doc comment on the file's first line reported | Not shared: Not reported |
| [#3656](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3656) | many | Fixes write CRLF into LF files | Not shared: All rules on a one-line LF file: no `\r` in the output |
| [#3564](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3564) | SA1501 -> BRO1508 | `catch { }` reported | Same or different by design: Same as StyleCop by design |
| [#3559](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3559) | SA1307 -> BRO1306 | `public static X _instance` not reported | Not shared: Reported, renamed to `Instance` |
| [#3557](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3557) | SA1304 -> BRO1306 | `protected readonly foo` never checked | Same or different by design: StyleCop's defaults matched on purpose (protected fields, see BRO1303) |
| [#3183](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3183) | SA1117 -> BRO1108 | Last-argument lambda exception | Same or different by design: same as StyleCop (maintainer: by design); the fix's indentation bug in the reported variant is #1620's, fixed |
| [#3096](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3096) | SA1623/SA1629 -> BRO1604/BRO1603 | Reported on internal members with documentInternalElements false | Same or different by design: Same design as StyleCop (validity of docs that exist) |
| [#3057](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3057) | SA1312 -> BRO1301 | `var _`, `foreach (var _ ...)`, `using var _` reported | Not shared: Not reported (`_`, `__`, `___`, `_3MonthsFromNow`) |
| [#2947](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2947) | SA1513 -> BRO1519 | `}` of a multi-line interpolation hole reported | Not shared: Not reported |
| [#2859](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2859) | SA1305 -> BRO1310 | Hungarian P/Invoke parameters reported | Shared, fixed: parameters of native methods aren't reported |
| [#2784](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2784) | SA1629 -> BRO1603 | Text ending in `<c>"... ."</c>` gets another period | Shared, fixed: not reported |
| [#2689](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2689) | SA1005 -> BRO1002 | `//-:cnd:noEmit` template markers reported | Shared, fixed: `//-:` and `//+:` aren't reported |
| [#1371](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1371) | SA1515 -> BRO1504 | Comment/doc right after `#region` reported | Not shared: Not reported (`///` exempt, line above a directive exempt). BRO1513 does want a blank line before docs after `#region`, matching SA1514 (probed earlier) |
| [#1450](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1450) | SA1119 -> BRO1405 | `(new[] { 1 }).ToArray()` reported | Same or different by design: Same as StyleCop (maintainers kept it) |
| [#1782](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/1782) | SA1612 -> BRO1611 | Stale + missing `<param>` both reported | Not shared: One BRO1611, fix renames the tag to `value` |
| [#3547](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3547) | SA1407 -> BRO1406 | `&`, `\|`, `^`, `>>>` not checked | Same or different by design: StyleCop's operator families on purpose |
| [#3167](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3167) | SA1408 -> BRO1407 | `a ?? false && b` not reported | Same or different by design: Feature request; same families as StyleCop |
| [#3955](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3955) | SA1201 -> BRO1001 | "A <unknown> should not follow a field" on C# 14 `extension` blocks | Not shared: Real build + `dotnet format` (SDK 10, C# 14): no BRO1001 in a type with an extension block (skipped, so also a field after a method there isn't sorted), no crash, compiles |
| [#2079](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/2079) | SA1309 -> BRO1303 | Rename misses a reference in another class | Same or different by design: The repro reads a private field from another class (doesn't compile) |
| Not planned [#3837](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3837) | SA1106 -> BRO1101 | `public class A;` reported as empty statement | Not shared: Not reported |
| Not planned [#3659](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3659) | SA1400 -> BRO1404/BRO1007 | `static file class X` asked for a modifier | Not shared: Not reported; a part without `file` of a `file` type isn't either (fixed 2026-10-04: BRO1007 added `internal` there, CS9052) |
| Not planned [#3716](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3716) | SA1119 -> BRO1405 | `#if (MyProperty)` (templates) reported | Not shared: Not reported |
| Not planned [#3769](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3769) | SA1300 -> BRO1309 | Record/primary-ctor parameters `name` want PascalCase | Not shared: Not reported |
