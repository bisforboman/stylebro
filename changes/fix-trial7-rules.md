### New

- `stylebro_private_mutable_static_field_naming = camelCase | _camelCase`: the style of private static fields that
  aren't readonly or const (StyleCop's SX1309S), when it differs from the other private fields'.
- `stylebro_keep_overloads_together = same_access`: overloads with the same accessibility, `static` and `abstract` stay
  together (Sonar's S4136).

### Changed

- **BRO1401** no longer checks property patterns, like StyleCop 1.2 (it put `},` after every nested pattern).
- **BRO1504** needs no blank line above a `//` comment inside a call chain.
- **BRO1611, BRO1613, BRO1614** skip members documented with `<inheritdoc>`, like StyleCop.
- **BRO1303, BRO1306-BRO1308** skip fields in `*NativeMethods` classes, like StyleCop (a lower-case `static readonly`
  field is still renamed).
- **BRO1606** replaces filler sentences (`Default constructor.`, `Creates a new instance of X.`) with the standard
  sentence; text after them stays.
- **BRO1142** follows `csharp_style_var_*`: split declarations use `var` where that preference asks for it, and aren't
  split where `var` can't be written.
- **BRO1108** with BRO1107 off keeps the first item on the `(` line.

### Fixed

- **BRO1001 broke code that reads members by position** through reflection (`GetFields()[0]`,
  `GetProperties().First()`): such a type keeps its order, and the finding says where the read is.
- A `nameof` keeps a name from being renamed only when it refers to that symbol, not to another one with the same name.
- `stylebro_blank_line_between_switch_sections = omit` was overruled by the blank-line rules after a `}` or before a
  comment.
- BRO1603 no longer adds a period after text that ends in code (`x = 1`, `a/b`); BRO1107/BRO1108 skip lists with a
  multi-line string.
- Two cases where the result depended on the order `dotnet format` ran the rules (member sorting with region removal,
  BRO1107 with BRO1108).
