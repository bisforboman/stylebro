### Changed

- `stylebro-migrate init` counts four more conventions: `//` comments right below code or after a blank line (BRO1504),
  doc comment texts with or without a closing period (BRO1603), keywords in doc comments as `<see langword="null"/>` or
  `<c>null</c>` (BRO1618), and non-private fields against an enforced naming rule (`dotnet_naming_rule.*`): when your
  code doesn't follow the rule, BRO1306 is turned off instead of renaming the fields to it.
- `stylebro-migrate init`: calls whose first argument is a multi-line lambda no longer count for BRO1107/BRO1108's
  conventions once such calls are allowed (`stylebro_allow_leading_multiline_lambda`), so the two rules stay on.
- `stylebro-migrate init` keeping one-line empty blocks also writes `csharp_preserve_single_line_statements = true`: without
  it, IDE0055 split `else { }` and `lock (x) { }` onto two lines.

### Fixed

- `stylebro-migrate --write` finds StyleCop referenced only for one target framework of a multi-targeted project
  (`<ItemGroup Condition="'$(TargetFramework)' == 'net6.0'">`). Such projects counted as "No StyleCop": they got no
  settings, `format` left them alone, and the build failed.
- `stylebro-migrate init` no longer turns IDE0055, IDE0036 and IDE0049 back on by id when your root `.editorconfig` turns
  the Style category (or every analyzer) off. Your own path sections (`[src/**/*.cs]` with IDE0055 `none`) keep their
  values: the block repeats them at its end.
- `stylebro-migrate init`: a folder that turns every analyzer off no longer gets its usings sorted by `format`. The using
  order key goes into a section for the rest of the code.
- `stylebro-migrate init` and `--write` no longer count usings inside `#if` as out of order (`format` doesn't sort across
  directives), no longer pin a preset version in fixture folders that only their own solution builds, and carry
  `SA1300` suppressions on types and members over without the namespace rule (BRO1312).
- `stylebro-migrate init --diff` on a StyleCop repository says to run the migration instead, without starting a preview.
- Report texts: `init` says when IDE0009 is enforced only in `.globalconfig` (`format` doesn't fix it there), no longer
  says `format` formats whitespace when your IDE0055 is `none`, and names "your settings" for an IDE0055 set in
  `.globalconfig`; the migration no longer calls missing StyleCop settings leftovers.
