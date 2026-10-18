# Configuration

Every `stylebro_*` setting, in `.editorconfig` under `[*.cs]`. Each rule's page explains its settings with examples;
this page lists them all in one place. Severities work like any analyzer's
(`dotnet_diagnostic.BRO1001.severity = none`), and the package's preset sets the defaults listed here unless your
`.editorconfig` says otherwise. Some rules also follow the SDK's own settings (`csharp_prefer_braces`, `csharp_using_directive_placement`,
`dotnet_style_qualification_for_*`, `dotnet_naming_rule.*`, `max_line_length`, ...): see their rule pages.

Which rules the preset turns on depends on its version, an MSBuild property (in `Directory.Build.props` or the project
file), not an `.editorconfig` key:

| `<StyleBroPreset>` | Rules |
|---|---|
| unset | the package's major version: `1.0` in all of 1.x |
| `1.0`, `1.1`, ... | the rules up to that release; newer releases' rules stay off |
| `latest` | every rule in the package |
| `none` | no preset: rules at their own defaults (rules added after 1.0 off), no preset settings |

A version newer than the package warns (STYLEBRO002) and uses the package's newest; any other value fails the build
(STYLEBRO001). See [versioning](versioning.md#new-rules).

These defaults are the `stylecop` style. Another style, `<StyleBroStyle>microsoft</StyleBroStyle>`, changes some of them
to dotnet/runtime's coding style: see [styles](styles.md).

```ini
[*.cs]
stylebro_null_check_style = equality_operator
```

| Key | Values | Default | Rules |
|-----|--------|---------|-------|
| `stylebro_member_order` | kinds, comma-separated: `field`, `constructor`, `finalizer`, `delegate`, `event`, `enum`, `interface`, `property`, `indexer`, `conversion`, `operator`, `extension`, `method`, `struct`, `class`, `record`; `a\|b` shares a place | that list (StyleCop's order; records count as classes unless `record` is listed) | [BRO1001](rules/BRO1001.md) |
| `stylebro_member_order_namespaces` | `true`, `false` (only a type's members are ordered) | `true` | [BRO1001](rules/BRO1001.md) |
| `stylebro_member_order_scope` | `all`, `fields_first` (only fields first in a type, nothing else ordered; the other order keys are ignored) | `all` | [BRO1001](rules/BRO1001.md) |
| `stylebro_member_access_order` | accessibilities, comma-separated: `public`, `internal`, `protected_internal`, `protected`, `private_protected`, `private` | that list | [BRO1001](rules/BRO1001.md) |
| `stylebro_member_constants_first` | `true`, `false` | `true` | [BRO1001](rules/BRO1001.md) |
| `stylebro_member_static_first` | `true`, `false` | `true` | [BRO1001](rules/BRO1001.md) |
| `stylebro_member_readonly_first` | `true`, `false` | `true` | [BRO1001](rules/BRO1001.md) |
| `stylebro_keep_overloads_together` | `true`, `false`, `same_access` | `false` | [BRO1001](rules/BRO1001.md) |
| `stylebro_allow_semicolon_after_closing_brace` | `true`, `false` (`true`: Sonar's S1116, the `;` after a type's or namespace's `}` stays) | `false` | [BRO1101](rules/BRO1101.md) |
| `stylebro_constructor_initializer_placement` | `own_line`, `same_line` | `own_line` | [BRO1105](rules/BRO1105.md) |
| `stylebro_empty_string_style` | `string_empty`, `literal` | `string_empty` | [BRO1106](rules/BRO1106.md) |
| `stylebro_split_list_first_item` | `next_line`, `same_line` | `next_line` | [BRO1107](rules/BRO1107.md), [BRO1108](rules/BRO1108.md) |
| `stylebro_allow_leading_multiline_lambda` | `true`, `false` (`true`: a first argument that is a multi-line lambda, anonymous method or anonymous object may keep the other arguments on its closing line) | `false` | [BRO1107](rules/BRO1107.md), [BRO1108](rules/BRO1108.md) |
| `stylebro_closing_parenthesis_placement` | `last_item`, `own_line` | `last_item` | [BRO1110](rules/BRO1110.md) |
| `stylebro_constraint_placement` | `own_line`, `same_line` | `own_line` | [BRO1111](rules/BRO1111.md) |
| `stylebro_null_check_style` | `pattern_matching`, `equality_operator` | `pattern_matching` | [BRO1133](rules/BRO1133.md), [BRO1148](rules/BRO1148.md) |
| `stylebro_upper_case_literal_suffixes` | `all`, `l_only`, `lone_l` (Sonar's S818: only a suffix that is just `l`) | `all` | [BRO1135](rules/BRO1135.md) |
| `stylebro_allow_null_forgiving_on_unannotated` | `true`, `false` (`true`: Sonar's S8969, a `!` on a value declared without nullable annotations stays) | `false` | [BRO1147](rules/BRO1147.md) |
| `stylebro_allow_nested_dynamic_ifs` | `true`, `false` (`true`: Sonar's S1066, nested `if`s with a `dynamic` condition aren't merged) | `false` | [BRO1149](rules/BRO1149.md) |
| `stylebro_params_arrays` | `all`, `except_object_and_jagged` (Sonar's S3878: not for `params object[]`, `params Array[]` or jagged arrays) | `all` | [BRO1151](rules/BRO1151.md) |
| `stylebro_object_creation_parentheses` | `omit`, `include` | `omit` | [BRO1141](rules/BRO1141.md) |
| `stylebro_private_field_naming` | `camelCase`, `_camelCase` | not set: the SDK's naming rules decide, else `camelCase` | [BRO1303](rules/BRO1303.md), [BRO1307](rules/BRO1307.md), [BRO1310](rules/BRO1310.md) |
| `stylebro_private_mutable_static_field_naming` | `camelCase`, `_camelCase`, `s_camelCase` | not set: a naming rule requiring just `static` decides, else `stylebro_private_field_naming`'s style | [BRO1303](rules/BRO1303.md) |
| `stylebro_allow_leading_underscore_in_pascal_fields` | `true`, `false` | `false` | [BRO1306](rules/BRO1306.md) |
| `stylebro_private_static_field_naming` | `PascalCase`, `camelCase`, `_camelCase`, `s_camelCase` (static readonly fields; constants stay PascalCase) | not set: the SDK's naming rules decide, else `PascalCase` | [BRO1306](rules/BRO1306.md) |
| `stylebro_internal_field_naming` | `PascalCase`, `like_private` (internal and private protected fields named like private ones) | `PascalCase` | [BRO1303](rules/BRO1303.md), [BRO1306](rules/BRO1306.md) |
| `stylebro_allowed_hungarian_prefixes` | prefixes, comma-separated | none | [BRO1310](rules/BRO1310.md) |
| `stylebro_allow_common_hungarian_prefixes` | `true`, `false` | `true` | [BRO1310](rules/BRO1310.md) |
| `stylebro_rename_public_api` | `true`, `false` | `false` | [BRO1302](rules/BRO1302.md)-[BRO1314](rules/BRO1314.md): names other assemblies see |
| `stylebro_tuple_element_name_casing` | `PascalCase`, `camelCase` | `PascalCase` | [BRO1311](rules/BRO1311.md) |
| `stylebro_match_base_parameter_names` | `all`, `non_generic_methods` (Sonar's S927: methods only, not where the base's parameter type is a type parameter the member fills in) | `all` | [BRO1313](rules/BRO1313.md) |
| `stylebro_allowed_namespace_components` | names, comma-separated | none | [BRO1312](rules/BRO1312.md) |
| `stylebro_trailing_comma` | `include`, `omit` | `include` | [BRO1401](rules/BRO1401.md), [BRO1509](rules/BRO1509.md) |
| `stylebro_trailing_comma_switch_expressions` | `true`, `false` (switch expressions not checked) | `true` | [BRO1401](rules/BRO1401.md) |
| `stylebro_trailing_comma_with_expressions` | `true`, `false` (`with` initializers not checked) | `true` | [BRO1401](rules/BRO1401.md) |
| `stylebro_comment_blank_line_exempt_prefixes` | prefixes, comma-separated, case-sensitive | none | [BRO1504](rules/BRO1504.md) |
| `stylebro_allow_adjacent_single_line_members` | `true`, `false` | `false` | [BRO1505](rules/BRO1505.md) |
| `stylebro_blank_line_below_standalone_comments` | `allow`, `remove` (like StyleCop's SA1512) | `allow` | [BRO1506](rules/BRO1506.md) |
| `stylebro_allow_empty_single_line_blocks` | `true`, `false` | `false` | [BRO1508](rules/BRO1508.md), [BRO1509](rules/BRO1509.md) |
| `stylebro_allow_consecutive_usings` | `true`, `false` | `true` | [BRO1514](rules/BRO1514.md) |
| `stylebro_allow_single_line_jump_statements` | `true`, `false` | `false` | [BRO1514](rules/BRO1514.md) |
| `stylebro_braces_statements` | `all`, `if_and_loops` (Sonar's S121: not `using`, `lock`, `fixed` or a deconstructing `foreach`) | `all` | [BRO1514](rules/BRO1514.md)-[BRO1516](rules/BRO1516.md) |
| `stylebro_arrow_placement_when_wrapping` | `end_of_line`, `beginning_of_line` | `end_of_line` | [BRO1521](rules/BRO1521.md) |
| `stylebro_equals_placement_when_wrapping` | `end_of_line`, `beginning_of_line` | `end_of_line` | [BRO1522](rules/BRO1522.md) |
| `stylebro_blank_line_between_switch_sections` | `include`, `omit`, `omit_after_block` | `include` | [BRO1526](rules/BRO1526.md) |
| `stylebro_document_exposed_elements` | `true`, `false` | `true` | [BRO1601](rules/BRO1601.md) |
| `stylebro_document_internal_elements` | `true`, `false` | `true` | [BRO1601](rules/BRO1601.md) |
| `stylebro_document_private_elements` | `true`, `false` | `false` | [BRO1601](rules/BRO1601.md) |
| `stylebro_summary_wording` | `stylecop`, `stylesharp` | `stylecop` | [BRO1604](rules/BRO1604.md), [BRO1605](rules/BRO1605.md), [BRO1606](rules/BRO1606.md), [BRO1607](rules/BRO1607.md) |
| `stylebro_inheritdoc_style` | `compact`, `spaced` | `compact` | [BRO1601](rules/BRO1601.md) |
| `stylebro_exclude_from_punctuation_check` | tag names, comma-separated | `seealso` | [BRO1603](rules/BRO1603.md) |
| `stylebro_file_header_company` | text | not set: the rule does nothing | [BRO1615](rules/BRO1615.md) |
| `stylebro_file_header_copyright` | text (`\n`, `{companyName}`, `{fileName}`) | StyleCop's copyright text | [BRO1615](rules/BRO1615.md) |
| `stylebro_file_header_decoration` | a line of text | none | [BRO1615](rules/BRO1615.md) |
| `stylebro_summary_layout` | `multi_line`, `single_line_when_fits` | `multi_line` | [BRO1616](rules/BRO1616.md) |
| `stylebro_layout_by` | `csharpier`, or not set | not set | [BRO1149](rules/BRO1149.md) lays a merged condition out as CSharpier would; [BRO1514](rules/BRO1514.md)-[BRO1516](rules/BRO1516.md) count a statement CSharpier will split as multi-line. `stylebro-migrate` writes it when the repository uses CSharpier ([getting started](getting-started.md)) |
| `stylebro_layout_print_width` | a number | `100` (CSharpier's) | with `stylebro_layout_by = csharpier`: CSharpier's `printWidth` |

Coming from StyleCop, [`stylebro-migrate`](migrating.md) writes these from your `stylecop.json` and rule settings.
