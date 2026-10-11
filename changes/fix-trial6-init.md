### Changed

- **Using order follows the code:** the preset no longer sets `dotnet_sort_system_directives_first` (`dotnet format`
  sorts usings whenever that key is set, to any value). `init` writes it when the code's usings are sorted (`true`, or
  `false` when System doesn't come first); for unsorted or mixed code it writes a comment on how to turn sorting on.
- **Line endings:** `init` writes `end_of_line = lf` or `crlf` when the C# files clearly use one, so `dotnet format`
  no longer writes the OS's line ending into the lines it touches. Files git converts on checkout
  (`core.autocrlf`, `.gitattributes` `text` without `eol`) don't count.
- **The repo's `.editorconfig` against its code:** when most of the code contradicts a key the repo sets (operator
  placement, `csharp_prefer_braces = true`, using placement, `always_for_clarity` parentheses), `init` keeps the key
  and turns off the StyleBro rule that would enforce it, with a comment saying why.
- **More conventions detected:** one-line `if`/loop/`case` statements (3 or more keep
  `csharp_preserve_single_line_statements = true`), `<inheritdoc/>` on overrides (BRO1601), and overloads kept together
  (`stylebro_keep_overloads_together`).
- **Running `init` again** keeps the convention lines the last run wrote; delete a line to have it counted again.

### Fixed

- A hand-written `Migrations` folder is formatted and counted like other code; only folders of real EF Core migrations
  are treated as generated.
- Sonar: S3260 no longer turns on CA1852, whose fix sealed classes that have `protected` constructors (CS0628 warnings);
  the report lists it as not covered. Suppressions only get the ids replacing Sonar rules that are on.
- `csharp_prefer_braces = when_multiline` is reported as a kept setting (the summary showed an empty rule name).

### Upgrading

If you relied on the preset to sort usings, run `stylebro-migrate init` again (it writes the key when your usings are
sorted) or set `dotnet_sort_system_directives_first = true` yourself.
