### Changed

- **BRO1504** no longer wants a blank line between `if (...)` (or `else`, a loop, `using`, `lock`, `fixed`) and a
  comment above the statement it runs without braces.
- **BRO1107**/**BRO1108** with `stylebro_allow_leading_multiline_lambda = true` also accept a leading lambda in
  parentheses (`Invoke((() => { ... }), 1)`) or wrapped in a delegate creation (`new Action(() => { ... })`).

### Fixed

- **BRO1311** no longer renames tuple element names in the signature of a public operator or conversion operator when
  `stylebro_rename_public_api` is false.
- **BRO1509** and **BRO1001** with BRO1505 off no longer add BRO1505's blank lines when they expand a type written on one
  line, so the result no longer depends on the order the fixes run in.
- **BRO1405** and **BRO1108** give the same result in any order on a leading lambda in parentheses.
- **BRO1104** and **BRO1150** write `default (T)` and `Array.Exists (...)` with a space where
  `csharp_space_between_method_call_name_and_opening_parenthesis = true`, so a second `dotnet format` run changes nothing.
