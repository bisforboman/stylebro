### New

- Settings that make six rules report exactly what their Sonar counterpart reports, so `stylebro-migrate` fixes those Sonar
  rules again (it stopped in the last version because these rules reported code the Sonar rules accept):
  - **BRO1514** `stylebro_braces_statements = if_and_loops` (Sonar's S121): `using`, `lock`, `fixed` and deconstructing
    `foreach` bodies need no braces. Applies to BRO1515 and BRO1516 too.
  - **BRO1135** `stylebro_upper_case_literal_suffixes = lone_l` (S818): only a suffix that is just `l` (`1l`); `1ul` and
    `1lu` are fine.
  - **BRO1149** `stylebro_allow_nested_dynamic_ifs` (S1066): nested `if`s with a `dynamic` condition aren't merged.
  - **BRO1101** `stylebro_allow_semicolon_after_closing_brace` (S1116): the `;` after a type's or namespace's `}` stays.
  - **BRO1151** `stylebro_params_arrays = except_object_and_jagged` (S3878): arrays for `params object[]`, `params Array[]`
    and jagged `params` arrays stay.
  - **BRO1147** `stylebro_allow_null_forgiving_on_unannotated` (S8969): a `!` on a value declared without nullable
    annotations (`#nullable disable`, unannotated libraries) stays.

### Changed (stylebro-migrate only)

- **Sonar mappings** are back for S121, S818, S1066, S1116, S3878 and S8969, each with the setting above. When the
  StyleCop setup turns the same rule on (SA1106, SA1503), StyleCop's wider scope stays and the report says so.
