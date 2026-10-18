### New

- **`stylebro_allow_leading_multiline_lambda`** (BRO1107, BRO1108; default `false`, like StyleCop): with `true`, a call whose
  first argument is a multi-line lambda, anonymous method or anonymous object may keep the other arguments on its closing
  line (`Register(ctx =>` / `{ ... }` / `}, SymbolKind.Field);`). `bro init` writes it when your code uses
  that layout.
- `bro init` detects more of your code's conventions: empty blocks `{ }` on one line
  (`stylebro_allow_empty_single_line_blocks`), properties with block-bodied accessors on one line (BRO1509), mixed accessor
  layouts (BRO1510) and partial type parts without an access modifier (BRO1007).

### Fixed

- `bro init` and `bro migrate --write` run from a subfolder (`src/`, or a `build/` folder holding the
  solution) work on the repository's root when the build reaches past that folder. Before, `init` from `src/` created a
  `src/Directory.Build.props` that hid the root's (StyleBro, `TreatWarningsAsErrors` and `Nullable` were lost), and from
  `build/` it wrote settings no project read. A `Directory.Build.props` StyleBro creates below another one now imports it.
- `bro init` reads the root `.globalconfig` like `.editorconfig`: an enforced `this.` (IDE0009) and the
  team's IDE0055 severity are kept. With `AnalysisMode` `All` and `EnforceCodeStyleInBuild`, a `this.` setting without a
  severity counts as enforced.
- `bro init` turns the built-in rules off by id in a folder whose `.editorconfig` turns the Style category
  off; the root block's IDE0055 used to reformat it.
- `bro migrate --write` with StyleCop spacing rules off (SA1000, SA1003, SA1008, SA1009): the SDK spacing option that
  keeps your code's spacing is written (`if(x)`, `a+b`, ...), and the report names what IDE0055 still changes where no
  option can express it (`new ()`).
- `bro migrate --write` for StyleCop 1.1.x: BRO1401 stays off when your code has multi-line switch expressions without
  a trailing comma (1.1.x's SA1413 doesn't check them).
- `bro migrate --write` no longer writes a block into a `root = true` folder without C# files (an npm app), and
  carries suppressions over only where StyleCop ran.
- `bro migrate --write` with the repository's own `dotnet_sort_system_directives_first`: only files whose sorted
  usings StyleCop's ordering rules would report are left unsorted; the others are sorted (StyleCop accepts that order).
- Report texts: the final-newline note says which files `format` changes; "Kept your style" names what was written;
  StyleSharp-only repositories get "Generated from this repository's StyleSharp settings"; the Sonar note no longer
  credits a StyleCop setup in `init`.
