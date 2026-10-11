# More options where preferences differ (2026-10-07)

## Question

Should rules offer more settings where teams reasonably prefer something else than StyleCop's behavior, and where
should they live: `.editorconfig` keys (as today), a `stylebro.json` like StyleCop's `stylecop.json`, or both?

## Choices

1. `.editorconfig` only: one place with the SDK's settings, per-folder sections, read natively by `dotnet format` and
   the IDEs; `stylebro-migrate` keeps translating `stylecop.json`.
2. `stylebro.json`: familiar to StyleCop users, but one file per project and a second place to look.
3. Both, `.editorconfig` winning.

Then a survey of every rule against the 12 public reference repositories listed candidates with usage counts.

## Decision

The owner: `.editorconfig` only, and all ten candidates plus the two higher-risk ones. Defaults stay StyleCop's
behavior; the preset doesn't set the new keys.

| Rule | Setting | Alternative |
|------|---------|-------------|
| BRO1104 | `csharp_prefer_simple_default_expression` (SDK key) | `default` instead of `default(T)` |
| BRO1106 | `stylebro_empty_string_style = string_empty \| literal` | `""` instead of `string.Empty` |
| BRO1135 | `stylebro_upper_case_literal_suffixes = all \| l_only` | only `l` must be upper case |
| BRO1401 | `stylebro_trailing_comma = include \| omit` | remove trailing commas |
| BRO1508/BRO1509 | `stylebro_allow_empty_single_line_blocks` | `{ }` may stay on one line |
| BRO1110 | `stylebro_closing_parenthesis_placement = last_item \| own_line` | `)` on its own line |
| BRO1107 | `stylebro_split_list_first_item = next_line \| same_line` | first item stays on the `(` line |
| BRO1505 | `stylebro_allow_adjacent_single_line_members` | single-line members without a blank line |
| BRO1601 | `stylebro_inheritdoc_style = compact \| spaced` | `<inheritdoc />` |
| BRO1105 | `stylebro_constructor_initializer_placement = own_line \| same_line` | `: base(x)` on the declaration line |
| BRO1111 | `stylebro_constraint_placement = own_line \| same_line` | `where` on the declaration line |

Considered and left out (nobody writes it the other way, or turning the rule off covers it): BRO1103, BRO1123,
BRO1136, BRO1102, BRO1146, one-line blocks with statements, and wording rules (BRO1603, BRO1606/BRO1607). The keys'
exact names and values are on the rule pages.
