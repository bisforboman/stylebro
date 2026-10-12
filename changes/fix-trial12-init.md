### Changed (stylebro-migrate only)

- **`stylebro-migrate init`** follows more of your code's conventions:
  - an `insert_final_newline` the C# files don't follow (last line break or not): the files' value is written for C#
    files, or `insert_final_newline = unset` when they're mixed, so `format` no longer adds or removes the last line
    break in every file (one repository's `false` removed it from 1,092 files). `stylebro-migrate --write` does the same,
    and now also for `charset`;
  - column alignment for ReSharper/Rider (`resharper_int_align_variables = true` and the other `int_align` keys): IDE0055
    is turned off, so its whitespace formatting doesn't flatten aligned code;
  - auto-properties written over several lines (`{` / `get;` / `set;` / `}`): mixed turns BRO1527 off;
  - `#region`s inside members (`#region Arrange` in tests): from 3 on, BRO1113 is turned off;
  - one-line jumps (`if (x) return;`): from 3 on, `stylebro_allow_single_line_jump_statements = true` keeps them without
    braces;
  - mixed code under a key nothing enforced (`dotnet_style_operator_placement_when_wrapping`, `csharp_prefer_braces`,
    using placement, arithmetic parentheses) turns the rule off, as when the code clearly contradicts the key.
- **`init` says** when your `.editorconfig`'s `dotnet_sort_system_directives_first` will make `format` reorder usings (it
  sorts whenever the key is set), and that `trim_trailing_whitespace = false` isn't read by `dotnet format`.

### Fixed

- **A second `init`** keeps a rule it turned off against your `.editorconfig`'s key, instead of rewriting the line with
  the new counts after `format`.
- **`init`** no longer writes a root `.editorconfig` when all the C# code is in folders whose `.editorconfig` says
  `root = true`, and the preview no longer adds a root `Directory.Build.props` just for a project without C# files.
- **`init`'s block** no longer says the file header is IDE0073's when `file_header_template = unset`.
