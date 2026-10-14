### Changed

- **`stylebro-migrate init`** turns [BRO1506](https://bisforboman.github.io/stylebro/rules/BRO1506/) off when your code
  mostly keeps a blank line below its `//` comments (or mixes both), like its other convention checks.
- **`stylebro-migrate init`** no longer turns IDE0009 off when your `.editorconfig` requires `this.`
  (`dotnet_style_qualification_for_* = true:warning`).
- A suppressed Sonar S4136 in `<NoWarn>` no longer turns BRO1001 off in that folder: **`init`** and **`--write`** set
  `stylebro_keep_overloads_together = false` there instead, and members are still ordered.

### Fixed

- BRO1603-BRO1607 and BRO1617-BRO1619 leave documentation with a bare `<` (`when a < b`) alone: BRO1603 put a period in
  the middle of the text (`a. < b`).
- Expanding a one-line method body (BRO1508/BRO1509) puts an `if`/loop body without braces on its own line when
  `csharp_preserve_single_line_statements = false`, so `format` no longer needs another run for it.
