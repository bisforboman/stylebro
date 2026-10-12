### Changed

- **StyleBro and CSharpier settle** in a repository that formats with CSharpier: after `stylebro-migrate format` and
  then `dotnet csharpier format .`, `stylebro-migrate format --verify-no-changes` is clean.
  - `stylebro-migrate init` also writes `stylebro_layout_by = csharpier` and CSharpier's line width
    (`stylebro_layout_print_width`, from the `.csharpierrc`'s `printWidth`, else `max_line_length`, else 100).
  - **BRO1149** then writes a merged condition the way CSharpier does: on the `if` line when it fits, otherwise each
    `&&` operand on a line of its own and `)` on its own line.
  - **BRO1514-BRO1516** count a statement CSharpier will split (longer than the line) as multi-line, so its braces come
    in the same run instead of after CSharpier.
  - Using directives follow CSharpier's order: where the `.editorconfig` sets `dotnet_sort_system_directives_first = false`
    (or `dotnet_separate_import_directive_groups = true`), `init` writes the opposite, since `dotnet format` sorts usings
    whenever the key is set and CSharpier sorts them System first.
- **`stylebro-migrate --write`** (from StyleCop) detects CSharpier like `init`: IDE0055 and the same layout rules are
  off, and the report lists the StyleCop rules only they replaced under "CSharpier decides it".

### Fixed

- **`stylebro-migrate format --verify-no-changes`** no longer says "Not clean" in a repository whose restore installs git
  hooks (Husky.Net's `dotnet husky install`, common next to CSharpier) when only findings StyleBro keeps on purpose are
  left.

### Upgrading

- In a repository that uses CSharpier, run `stylebro-migrate init --write` (or `--write`) again to get the two new keys.
