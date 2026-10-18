# Proposal: the Microsoft style

Status: **built** (2026-10-16; user docs: [styles.md](../styles.md)) (owner's decisions of 2026-10-16:
[source](../decisions/2026-10-16-microsoft-style-the-source-is-dotnetruntimes-coding-style.md),
[selection](../decisions/2026-10-16-microsoft-style-selected-with-stylebrostyle-and-init-style.md),
[design first](../decisions/2026-10-16-microsoft-style-a-design-document-first.md)). All seven questions at the end are
answered (A each, [answers](../decisions/2026-10-16-microsoft-style-open-questions-answered.md)). Gaps G1-G3 are built
(section 4); the layer and `init --style microsoft` are built. Where the build differs from this design: CA1852 is a
suggestion (the SDK's fix seals types with `virtual` members: CS0549 in Newtonsoft.Json's tests), CA1052 checks only
private and internal types, and a code base that uses `this.` gets IDE0003 off (IDE0009 isn't turned on).

StyleBro's preset follows StyleCop's defaults. A large share of .NET code follows Microsoft's own style instead: the one
dotnet/runtime writes down. This proposal adds that style as a layer on top of the preset:

```xml
<!-- Directory.Build.props -->
<StyleBroPreset>1.0</StyleBroPreset>
<StyleBroStyle>microsoft</StyleBroStyle>
```

and `bro init --style microsoft`, which writes that property and the IDE severities the style needs.

## Sources

Read at dotnet/runtime `main`, commit `24543ddd` (2026-10-16):

- [`docs/coding-guidelines/coding-style.md`](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md):
  20 numbered rules and an example file. Called "the guide" below.
- [`.editorconfig`](https://github.com/dotnet/runtime/blob/main/.editorconfig) at the root: formatting options, code-style
  preferences (mostly `:suggestion`), three naming rules, the license header.
- Found on the way, not one of the owner's two sources:
  [`eng/CodeAnalysis.src.globalconfig`](https://github.com/dotnet/runtime/blob/main/eng/CodeAnalysis.src.globalconfig),
  which runtime's build adds to every source project (`eng/Analyzers.targets`) together with a **StyleCop.Analyzers**
  reference. It sets every SA rule's severity (most `none`) and the IDE/CA severities runtime enforces (IDE0036, IDE0049,
  IDE0065, IDE0073, CA1507, CA1852 at `warning`; IDE1006, IDE0011, IDE0009 at `silent`). It answers what the guide leaves
  open for StyleBro's StyleCop rules (question 1). Called "runtime's build" below.

Precedence when they disagree: the guide, then the root `.editorconfig`, then runtime's build. Example: the
`.editorconfig` has `csharp_prefer_braces = true:silent`, the guide's rule 18 makes braces optional on one-line bodies,
so the style follows rule 18.

## 1. Mapping

Categories:

- **StyleBro**: an existing StyleBro rule or option covers it; the layer sets the value shown.
- **SDK**: a built-in option or IDE/CA rule covers it; options go in the layer, severities in the `.editorconfig` block
  `init --style microsoft` writes (`dotnet format` ignores severities in a package's global config).
- **default**: the preset already does exactly this; the layer sets nothing.
- **gap**: nothing in StyleBro or the SDK enforces and fixes it (section 4).
- **conflict**: a preset rule works against it and the layer turns it off.
- n/a: repository-specific, not part of a style.

### 1a. The guide

| # | Guide rule | Category | Layer / init sets | Notes |
|---|---|---|---|---|
| 1 | Allman braces | default | (preset: `csharp_new_line_before_open_brace = all`) | IDE0055 |
| 1, 18 | A one-line body may go without braces; braces when the body spans lines or another block of the chain has them | StyleBro | `csharp_prefer_braces = when_multiline` | BRO1514 then reports nothing, BRO1515 (multi-line body) and BRO1516 (inconsistent chain) stay on: exactly rule 18's two "required" cases |
| 1 | `using` nested in `using` at the same indentation | default | (`stylebro_allow_consecutive_usings = true`) | |
| 2 | Four spaces, no tabs | default | - | `indent_style`/`indent_size` are per-file keys; `init` writes them from the code |
| 3 | Private and internal instance fields `_camelCase` | StyleBro + gap | `stylebro_private_field_naming = _camelCase`, `stylebro_internal_field_naming = like_private` (G2) | Private: BRO1303. Internal: BRO1306 makes them PascalCase (StyleCop's SA1307) unless an *enforced* naming rule says otherwise, and runtime's naming rules are `suggestion` under `IDE1006 = silent`, which StyleBro counts as not enforced. Needs gap G2 |
| 3 | Private and internal static fields `s_camelCase` (also `static readonly`) | gap | `stylebro_private_static_field_naming = s_camelCase`, `stylebro_private_mutable_static_field_naming = s_camelCase` (G1) | Today BRO1303/BRO1306 leave `s_` names alone but rename an unprefixed static field to `_count`/`Count`: a conflict until G1 exists |
| 3 | Thread-static fields `t_` | gap (G8) | - | `[ThreadStatic]` fields are attributed, so BRO1303/BRO1306 already leave them alone; nothing adds `t_` |
| 3 | `readonly` where possible | SDK | `dotnet_style_readonly_field = true`; IDE0044 `suggestion` | |
| 3 | `static readonly`, not `readonly static` | default | (preset modifier order) | IDE0036 |
| 3 | Public fields PascalCase, no prefix | default | - | BRO1306 |
| 4 | No `this.` unless necessary | SDK | (preset: `dotnet_style_qualification_for_* = false`); IDE0003 `warning` | Today's template only turns IDE0009 off; nothing removes `this.`. BRO1303's fix already drops a `this.` it no longer needs |
| 5 | Visibility always written | default | (`dotnet_style_require_accessibility_modifiers = for_non_interface_members`) | BRO1404, BRO1007 |
| 5 | Visibility first | default | (preset `csharp_preferred_modifier_order`, identical to runtime's) | IDE0036 |
| 6 | Usings outside the namespace | default | (`csharp_using_directive_placement = outside_namespace`) | BRO1008 |
| 6 | Usings sorted, `System.*` first | SDK | `dotnet_sort_system_directives_first = true` in `.editorconfig`, **not** the layer | Setting the key at any value makes `dotnet format` sort, and an `.editorconfig` can't unset a global config's key, so a repository whose code isn't sorted could never opt out. `init` writes it unless the code clearly isn't sorted (as today) |
| 7 | At most one blank line | default | - | BRO1517 |
| 8 | No spurious spaces | default | - | IDE0055 (whitespace pass trims) |
| 9 | A file's existing style wins | gap (G9) | - | StyleBro decides per repository (and per folder through `.editorconfig`), not per file; not planned |
| 10 | `var` only when the type is named on the right; never required | SDK | `csharp_style_var_for_built_in_types = false`, `csharp_style_var_when_type_is_apparent = false`, `csharp_style_var_elsewhere = false`; IDE0008 `suggestion`, IDE0007 `none` | IDE0008 at `warning` is unsafe in multi-targeted projects (the type can differ per framework); runtime itself has it at `suggestion` |
| 10 | Target-typed `new()` only when the type is named on the left of a declaration | gap (G7) | - | IDE0090 suggests `new()` only where the type is apparent, but nothing turns `x = new(...)` back |
| 11 | Keywords, not BCL names (`int.Parse`) | default | (preset `dotnet_style_predefined_type_*`) | IDE0049 |
| 12 | Constant fields PascalCase; interop constants keep their native name | default | - | BRO1306 (skips SCREAMING_CASE and interop names) |
| 12 | Constant locals PascalCase | gap (G4) | - | BRO1301 leaves local constants alone; nothing renames them |
| 13 | Methods and local functions PascalCase | default | - | BRO1309 |
| 14 | `nameof(...)` instead of `"..."` | SDK | CA1507 `warning` | As in runtime's build |
| 15 | Fields at the top of the type | gap (G3) | `stylebro_member_order_scope = fields_first` | BRO1001 sorts every kind and accessibility; runtime's build has SA1201-SA1204 off. A fields-first-only mode would cover rule 15 exactly |
| 16 | Non-ASCII characters as `\uXXXX` | gap (G6) | - | |
| 17 | Labels one level less | default | (`csharp_indent_labels = one_less_than_current`, the SDK default) | IDE0055 |
| 18 | Never `if (x) return;` on one line | gap (G5) | - | `csharp_preserve_single_line_statements = false` is about two statements on a line; to be probed whether IDE0055 moves an embedded statement. `stylebro_allow_single_line_jump_statements` only matters where braces are required |
| 19 | Internal and private types `sealed` or `static` | SDK | CA1852 `warning`, CA1052 `warning` | As in runtime's build |
| 20 | Primary-constructor parameters camelCase, no `_` | default | - | BRO1302 |
| Example | `if (items == null)` | conflict | `dotnet_diagnostic.BRO1133.severity = none` | BRO1133's default turns it into `is null`; runtime doesn't prefer either (IDE0041 only replaces `ReferenceEquals`) |
| Example | `get { return _count; }` in a multi-line property | default | - | BRO1509/BRO1510 allow it |

### 1b. The root `.editorconfig`

| Keys | Category | Layer / init sets | Notes |
|---|---|---|---|
| `[*]` `insert_final_newline`, `indent_style`, `indent_size`, `trim_trailing_whitespace` | default | - | Per-file keys: `init` already writes them from the code |
| `charset` for project files, `generated_code` globs, `[*.cpp]`, XML/JSON sections | n/a | - | Not C#, or runtime's own files |
| `csharp_new_line_before_*` (open brace, else, catch, finally, object initializer members, anonymous type members), `csharp_new_line_between_query_expression_clauses` | default | - | The preset's values or the SDK defaults; BRO1128 agrees with the query one |
| `csharp_indent_block_contents`, `_braces`, `_case_contents`, `_switch_labels`, `_labels` | default | - | SDK defaults |
| `csharp_indent_case_contents_when_block = false` | SDK | `csharp_indent_case_contents_when_block = false` | Not the SDK default; init's existing "case blocks" convention still lets the code decide |
| `csharp_preferred_modifier_order` | default | - | Same list as the preset |
| `dotnet_style_qualification_for_* = false` | SDK | see guide rule 4 | |
| `csharp_style_var_*` | SDK | see guide rule 10 | |
| `dotnet_style_predefined_type_*` | default | - | |
| Naming rule: constant fields PascalCase | default | - | BRO1306 |
| Naming rule: private/internal/private protected static fields `s_` camelCase | gap (G1) | - | See guide rule 3. The layer copies no `dotnet_naming_rule.*` (a repository's own naming rules would merge with them by name); it uses `stylebro_*` keys |
| Naming rule: private/internal fields `_` camelCase | StyleBro + gap (G2) | `stylebro_private_field_naming = _camelCase` | See guide rule 3 |
| `csharp_using_directive_placement`, `dotnet_sort_system_directives_first` | default / SDK | see guide rule 6 | |
| `csharp_prefer_braces = true:silent` | StyleBro | `when_multiline` (guide rule 18 wins) | |
| `csharp_preserve_single_line_blocks = true`, `csharp_preserve_single_line_statements = false` | default | - | Same as the preset |
| `csharp_prefer_static_local_function = true` | SDK | the key; IDE0062 `warning` | `warning` in runtime's build |
| `csharp_prefer_simple_using_statement = false` | SDK | the key; IDE0063 `none` | Wins over `init --modernize`'s IDE0063 (see section 3) |
| `csharp_style_prefer_switch_expression = true` | SDK | the key; IDE0066 `suggestion` | |
| `dotnet_style_object_initializer`, `dotnet_style_collection_initializer` | SDK | the keys; IDE0017/IDE0028 `suggestion` | |
| `dotnet_style_prefer_collection_expression = when_types_exactly_match` | SDK | the key | IDE0300-IDE0305 at their defaults |
| `dotnet_style_explicit_tuple_names = true` | default | - | BRO1124 (SA1142, on in runtime's build) does the same |
| `dotnet_style_coalesce_expression`, `dotnet_style_null_propagation` | SDK | the keys; IDE0029/IDE0030 `warning`, IDE0031 `silent` | Runtime: IDE0031 silent because of TreatWarningsAsErrors |
| `dotnet_style_prefer_is_null_check_over_reference_equality_method = true` | SDK | the key; IDE0041 `warning` | |
| `dotnet_style_prefer_inferred_tuple_names`, `_anonymous_type_member_names` | SDK | the keys; IDE0037 `suggestion` | |
| `dotnet_style_prefer_auto_properties = true` | SDK | the key; IDE0032 `suggestion` | |
| `dotnet_style_prefer_conditional_expression_over_*` (`silent`) | SDK | the keys | Hints only |
| `csharp_prefer_simple_default_expression = true` | StyleBro | the key | BRO1104 follows it (`default`, not `default(T)`); IDE0034 `suggestion` |
| `csharp_style_expression_bodied_*` (all `true:silent`) | SDK | the keys | Hints only; no StyleBro rule cares |
| `csharp_style_pattern_matching_over_*`, `csharp_style_inlined_variable_declaration` | SDK | the keys; IDE0019/IDE0018 `suggestion`, IDE0020 `warning` | |
| `csharp_style_throw_expression`, `csharp_style_conditional_delegate_call` | SDK | the keys; IDE0016/IDE1005 `suggestion` | |
| `csharp_style_prefer_index_operator = false`, `_range_operator = false`, `csharp_style_pattern_local_over_anonymous_function = false` | SDK | the keys; IDE0056/IDE0057/IDE0039 `none` | Win over `init --modernize` |
| `dotnet_diagnostic.IDE0071/IDE0031.severity = silent` | SDK | the same | |
| `csharp_space_*` (22 keys) | default | - | All SDK defaults |
| `file_header_template` (.NET Foundation MIT text) | n/a | - | The .NET Foundation's header; a style must not stamp it on other code. BRO1615 stays inactive without a company, IDE0073 stays the user's choice |
| `xUnit1051`, `CA2025` | n/a | - | Runtime's own workarounds |

### 1c. StyleBro rules the guide and `.editorconfig` don't mention

The preset turns on many StyleCop-derived rules neither source talks about (trailing commas, `string.Empty`, list layout,
documentation wording, ...). Runtime's build answers each through its StyleCop severities. Question 1 decides whether
the layer follows it; under the recommended answer:

- **Stay on** (on in runtime's build, or required by the guide): BRO1003, BRO1004, BRO1006, BRO1007, BRO1008, BRO1104,
  BRO1116, BRO1117, BRO1119, BRO1121, BRO1123, BRO1124, BRO1127-BRO1130, BRO1302, BRO1303, BRO1304, BRO1306, BRO1309,
  BRO1402, BRO1403, BRO1404, BRO1507, BRO1512, BRO1514-BRO1517, BRO1608, BRO1609, BRO1612-BRO1614, BRO1615 (inactive
  without a company).
- **Off** (`none` in runtime's build: SA1005, SA1004, SA1106, SA1133, SA1131, SA1128, SA1122, SA1116, SA1117, SA1110,
  SA1111, SA1127, SA1123, SA1132, SA1125, SA1114, SA1120, SA1139, SA1130, SA1135, SA1100, SA1108, SA1312, SA1314, SA1308,
  SA1310, SA1316, SA1413, SA1119, SA1407, SA1408, SA1509, SA1510, SA1505, SA1515, SA1516, SA1512, SA1501, SA1502,
  SA1504, SA1506, SA1514, SA1508, SA1513, SA1600, SA1626, SA1629, SA1623, SA1624, SA1642, SA1643, SA1627, SA1612):
  BRO1001, BRO1002, BRO1005, BRO1101-BRO1103, BRO1105-BRO1115, BRO1118, BRO1120, BRO1122, BRO1125, BRO1126, BRO1131,
  BRO1132, BRO1301, BRO1305, BRO1307, BRO1308, BRO1311, BRO1401, BRO1405-BRO1407, BRO1501-BRO1506, BRO1508-BRO1511,
  BRO1513, BRO1518, BRO1519, BRO1601-BRO1607, BRO1610, BRO1611.
- **Off, beyond StyleCop** (no SA id, so runtime's build has no opinion; `bro migrate` turns these off too):
  BRO1133-BRO1151, BRO1410, BRO1520-BRO1527, BRO1616-BRO1619. BRO1310/BRO1312-BRO1314/BRO1409/BRO1143/BRO1145/BRO1147/
  BRO1148 are already off.

This list is what `bro migrate --write` derives from runtime's build (the same SA-to-BRO mapping, `data/mapping.csv`),
plus the guide's overrides (BRO1008, BRO1302, BRO1303, BRO1306, BRO1309, BRO1514-BRO1517 on). Building the layer can
generate it that way and commit the result, so the reasoning is reproducible.

### Counts

Rows of 1a and 1b (33 guide rows, 35 `.editorconfig` rows; a row with two categories counts in both):

| Category | Guide | `.editorconfig` | Total |
|---|---|---|---|
| StyleBro (existing rule/option, the layer sets it) | 2 | 3 | 5 |
| SDK (option or IDE/CA severity) | 6 | 19 | 25 |
| default (preset already does it) | 16 | 10 | 26 |
| gap | 9 | 2 | 11 |
| conflict (rule turned off) | 2 | 0 | 2 |
| n/a | 0 | 3 | 3 |

The gap rows are 9 distinct items (G1-G9, section 4; the naming rows of 1b repeat G1 and G2). G1 and G2 are also
conflicts until they exist: today's rules rename those fields the wrong way.

## 2. How the layer works

### Files and precedence

```
build/presets/stylebro.1.0.globalconfig          global_level = -2   (today -1)
build/presets/stylebro.1.1.globalconfig          global_level = -2
build/presets/styles/microsoft.1.0.globalconfig  global_level = -1
build/presets/styles/microsoft.1.1.globalconfig  global_level = -1
```

- A style layer overrides preset keys (`dotnet_diagnostic.BRO1001.severity = none`). Two global configs at the same
  `global_level` that set one key conflict and the key is dropped (Roslyn warns), so the style needs a level above the
  preset. The preset moves from -1 to -2 and the style takes -1: everything the repository sets (its `.editorconfig`, its
  `.globalconfig` at level 100, other global configs at 0) still wins over both. No user-visible change for repositories
  without a style.
- One style layer per preset minor, only where the style has something to say about that minor's new rules. The targets
  add `styles/<style>.X.Y.globalconfig` for every X.Y up to `_StyleBroPresetLevel`, the same condition as the preset
  layers, so a style never turns on a rule the chosen preset version doesn't have.
- The PresetVersionTests rules apply per style: no key in two layers of one style; every key it sets is a rule or option
  that exists; a layer for a minor only names rules added in that minor (or settings, see versioning).

### Values and diagnostics

| `<StyleBroStyle>` | Effect |
|---|---|
| unset or `none` | no style layer (today's behaviour) |
| `microsoft` | the Microsoft layers up to the preset version |
| anything else | build error **STYLEBRO003** naming the valid styles (a new code: STYLEBRO001's text is about preset versions) |

With `<StyleBroPreset>none</StyleBroPreset>` a style has nothing to sit on (and `migrate --write` writes `none` for its
own complete block): the style is ignored with warning **STYLEBRO004** ("StyleBroStyle is ignored with StyleBroPreset
none"). STYLEBRO002 (preset newer than the package) is unchanged; the style follows the preset level it resolves to.

### Versioning (docs/versioning.md)

From 1.0 the style counts as part of the preset's defaults:

- **Patch**: fix a mapping that contradicts the sources and makes a rule report less (turn off a rule the style should
  never have had on).
- **Minor**: the style's decision on new rules and settings, in that minor's style layer (new rules stay off unless the
  preset version includes them, like everyone else).
- **Major**: change the style's value for an existing rule or setting, including following a change in runtime's guide
  (question 5). The major folds the style's layers like the preset's.

The style page records the runtime commit it was written from, and lists runtime changes not yet followed.

### `bro migrate --write`

`--write` maps a StyleCop/StyleSharp/Sonar setup and writes `StyleBroPreset=none` plus a complete block, so a style
would only be ignored there (STYLEBRO004). `--write` doesn't write `StyleBroStyle` and, when the repository already has
it, reports that the migrated block replaces it and removes it (the same way it sets `none`). `--write --style` is
rejected: a migrated repository follows its own StyleCop settings.

## 3. What `init --style microsoft` writes

1. `<StyleBroPreset>X.Y</StyleBroPreset>` (as today) and `<StyleBroStyle>microsoft</StyleBroStyle>` in the root
   `Directory.Build.props` (`Migration.SetPreset` gets the second property).
2. The `.editorconfig` block from `sdk-rules.editorconfig`, with the style's differences:

   | Key | Template today | `--style microsoft` |
   |---|---|---|
   | `dotnet_diagnostic.IDE0003.severity` | - | `warning` (remove `this.`) |
   | `dotnet_diagnostic.IDE0009.severity` | `none` | `none` (unchanged) |
   | `dotnet_diagnostic.IDE0008.severity` | - | `suggestion` |
   | `dotnet_diagnostic.IDE0007.severity` | - | `none` |
   | `dotnet_diagnostic.CA1507.severity`, `CA1852`, `CA1052` | - | `warning` |
   | `dotnet_diagnostic.IDE0020/IDE0029/IDE0030/IDE0041/IDE0062.severity` | - | `warning` (runtime's build) |
   | `dotnet_diagnostic.IDE0017/IDE0018/IDE0019/IDE0028/IDE0032/IDE0034/IDE0037/IDE0044/IDE0066/IDE1005/IDE0016.severity` | - | `suggestion` |
   | `dotnet_diagnostic.IDE0063/IDE0056/IDE0057/IDE0039.severity` | - | `none` |
   | `dotnet_sort_system_directives_first` | when the code sorts | `true` unless the code clearly doesn't sort |

   Which severities exactly is question 2. Every new IDE/CA id at `warning` needs the multi-target bisect
   (`scratchpad/bisect-ide.ps1`) before it ships; one that breaks multi-targeted repositories joins
   `InitCommand.UnsafeWhenMultiTargeted` (written as `suggestion` there).
3. `format` then fixes the BRO ids plus these IDE/CA ids, as today.

### Conventions still win

`init` compares each convention's count with a default (StyleBro's or the preset's) and writes the other value when the
code clearly (>= 75 % of >= 10 places) uses it. With `--style microsoft` the default is the style's value, so:

| Code | Style says | `init` writes |
|---|---|---|
| `private int count;` in 90 % of 200 fields | `_camelCase` | `stylebro_private_field_naming = camelCase` |
| Braces on 95 % of one-line bodies | `when_multiline` | `csharp_prefer_braces = true` |
| Static fields without `s_` (existing "private static fields" convention) | `s_camelCase` (after G1) | the unprefixed style |
| Usings not sorted | sorted, System first | no `dotnet_sort_system_directives_first` |
| Members in BRO1001's order in 90 % of types | BRO1001 off | BRO1001 `warning` |
| `this.` on 85 % of member accesses | no `this.` | `dotnet_style_qualification_for_* = true`, IDE0009 `warning`, IDE0003 `none` |

The last row needs a new convention (count `this.`-qualified vs unqualified instance member accesses); today `init` only
keeps an *enforced* `this.` rule (`RequiresThisQualification`). The report says, per convention, "the Microsoft style
says X, your code clearly uses Y: kept Y".

## 4. Gaps

| Id | Runtime rule | What would close it | Estimate |
|---|---|---|---|
| G1 | `s_` on private/internal static fields (guide 3, naming rule) | **Done** (2026-10-16): `s_camelCase` as a value of `stylebro_private_static_field_naming` (static readonly; constants stay PascalCase) and `stylebro_private_mutable_static_field_naming`; internal static fields through G2's key; BRO1307 keeps `s_` and turns other one-letter prefixes into it; init counts a set `s_camelCase` as the team's decision (writing it as the style's default is the `init --style` work) | - |
| G2 | `_camelCase` internal instance fields (guide 3) | **Done** (2026-10-16): `stylebro_internal_field_naming = PascalCase \| like_private`; `like_private` names internal and private protected fields by the private keys (BRO1303 for instance and mutable static fields, BRO1306 for constants and static readonly) and wins over unenforced naming rules | - |
| G3 | Fields first, nothing else ordered (guide 15) | **Done** (2026-10-16): BRO1001 option `stylebro_member_order_scope = all \| fields_first` (fields above other members, both groups keep their order; the other order keys are ignored) | - |
| G4 | PascalCase constant locals (guide 12) | BRO1301 checks local constants against a casing option | small |
| G5 | No `if (x) return;` on one line (guide 18) | probe IDE0055 first; else a layout rule moving an embedded statement to its own line | small |
| G6 | Non-ASCII as `\uXXXX` (guide 16) | new rule escaping non-ASCII characters in regular string and char literals (skip verbatim/raw strings and comments) | small-medium |
| G7 | `new()` only in declarations (guide 10) | new rule writing the type back into `x = new(...)` assignments | small, low priority |
| G8 | `t_` on thread-static fields (guide 3) | rename `[ThreadStatic]` fields (attributes block renames today) | low priority, skip for now |
| G9 | A file's own style wins (guide 9) | per-file style detection | not planned: StyleBro is per repository/folder |

G1 and G2 are needed before the style can ship: without them the style renames runtime-shaped static and internal
fields the wrong way. G3 is wanted (otherwise rule 15 isn't checked at all). The rest can follow.

## 5. Questions (all answered A on 2026-10-16)

### 1. Which value do StyleBro rules get that the guide and `.editorconfig` don't mention?

| Option | Example: `string.Empty` (BRO1106), trailing commas (BRO1401), list layout (BRO1107/BRO1108) |
|---|---|
| **A. Runtime's build (`eng/CodeAnalysis.src.globalconfig`) decides; rules beyond StyleCop off** | All three off (SA1122, SA1413, SA1116/SA1117 are `none` there) |
| B. The preset's value stays | All three on: runtime-style code gets StyleCop's layout on top |
| C. Everything not named in the two sources off | All three off, but also BRO1121/BRO1608/... that runtime's build has on |

**Answered 2026-10-16: A** (the owner; it was the recommendation). It is what runtime's own code is checked against, so a runtime-shaped repository gets few
surprises, and it is mechanical (`migrate`'s mapping over runtime's file, section 1c).

### 2. Which severities does `init --style microsoft` write for the IDE options?

| Option | IDE0044 (`readonly` fields) |
|---|---|
| **A. Runtime's build severities for the ids whose options the style sets** | `suggestion`: shown in the IDE, not applied by `dotnet format` |
| B. `warning` for everything | applied by `dotnet format`: more churn, more ids to bisect for multi-targeting |
| C. Only the guide's numbered rules at `warning`, nothing else | IDE0044 not written |

**Answered 2026-10-16: A** (the owner; it was the recommendation). It mirrors what Microsoft enforces; StyleBro's "everything reported is fixed" holds because
`suggestion` isn't reported by `dotnet format --severity warn`.

### 3. Should `init` without `--style` detect a runtime-style repository and suggest the style?

| Option | `init` on a repo with `_camelCase` + `s_` fields and no `this.` |
|---|---|
| **A. Suggest it in the report** | "Your code looks like the Microsoft style (`s_` statics, `_` fields): try `init --style microsoft`" |
| B. Apply it automatically | `StyleBroStyle` written without being asked |
| C. No | nothing |

**Answered 2026-10-16: A** (the owner; it was the recommendation). The signals are cheap (init already counts both conventions) and choosing a style stays the team's
decision. The owner: `init` only suggests the style in its report, it never applies it.

### 4. How should the property value be named?

| Option | `.csproj` |
|---|---|
| **A. `microsoft`** | `<StyleBroStyle>microsoft</StyleBroStyle>` |
| B. `dotnet-runtime` | `<StyleBroStyle>dotnet-runtime</StyleBroStyle>` |

**Answered 2026-10-16: A** (the owner; it was the recommendation): the owner's working name, and what people call it, with the docs saying it is dotnet/runtime's
coding style. A later variant gets its own name (question 6).

### 5. How does the style follow changes to runtime's guide?

| Option | Runtime starts requiring `is null` |
|---|---|
| **A. Pinned per StyleBro major** | Listed as a known difference until StyleBro 2.0 |
| B. Each minor's style layer may change values, behind the preset version | Changes when the team raises `StyleBroPreset` (needs a `global_level` per minor: more machinery) |
| C. Follow in any release | Upgrading the package changes a build: breaks the versioning promise |

**Answered 2026-10-16: A** (the owner; it was the recommendation). It matches docs/versioning.md (changing defaults is a major) with no new machinery.

### 6. Should a "Microsoft Learn conventions" variant exist later?

| Option | |
|---|---|
| **A. Not now; revisit when someone asks** | `microsoft` only |
| B. Plan it now as `<StyleBroStyle>microsoft-learn</StyleBroStyle>` | A second style to map and maintain from prose without an `.editorconfig` |

**Answered 2026-10-16: A** (the owner; it was the recommendation). Most of what the Learn pages say matches runtime's guide; a second style can reuse the layer
mechanism when there is demand.

### 7. Ship before or after 1.0?

| Option | |
|---|---|
| **A. After G1-G3, independent of 1.0** (before 1.0: one `microsoft.1.0` layer; after: per-minor layers) | |
| B. Only after 1.0 | |

**Answered 2026-10-16: A** (the owner; it was the recommendation). The mechanism is the preset's, already shipped; a trial on runtime-style repositories (dotnet/aspire,
dotnet/aspnetcore-shaped code) belongs in the "two clean trials" before it ships. The owner: the work starts after the trial 24 fix PRs merge.
