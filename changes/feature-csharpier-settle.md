### Changed

- **StyleBro and CSharpier settle** in a repository that formats with CSharpier: after `stylebro-migrate format` and
  then `dotnet csharpier format .`, `stylebro-migrate format --verify-no-changes` is clean.
  - `stylebro-migrate init` also writes `stylebro_layout_by = csharpier` and CSharpier's line width
    (`stylebro_layout_print_width`, from the `.csharpierrc`'s `printWidth`, else `max_line_length`, else 100).
  - **BRO1149** then writes a merged condition the way CSharpier does: on the `if` line when it fits, otherwise each
    `&&` operand on a line of its own and `)` on its own line.
  - **BRO1514-BRO1516** count a statement CSharpier will split (longer than the line) as multi-line, so its braces come
    in the same run instead of after CSharpier.
- **`stylebro-migrate --write`** (from StyleCop) detects CSharpier like `init`: IDE0055 and the same layout rules are
  off, and the report lists the StyleCop rules only they replaced under "CSharpier decides it".

### Upgrading

- In a repository that uses CSharpier, run `stylebro-migrate init --write` (or `--write`) again to get the two new keys.
