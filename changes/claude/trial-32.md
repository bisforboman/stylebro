### Fixed

- **init** and **migrate** keep the line ending of a file that holds a string literal spanning lines (`@"..."`,
  `"""..."""`) when they write `end_of_line` for the other ending: the file gets a section of its own with its ending.
  Before, `format` converted it and changed the literal's value, and tests comparing text with it failed.
- **migrate** keeps `case X:` blocks written at the label's level (`csharp_indent_case_contents_when_block = false`), as
  init does: StyleCop doesn't check their indentation, and `format` re-indented them.
- **`bro format`** formats projects that reach a multi-targeted project only through another project's reference (a
  library for one framework that references one for one framework that references a multi-targeted one). Before, the
  run for a framework only the last one has skipped them, and every project referencing them, as "references didn't load".
- **BRO1504** no longer puts a blank line between a trailing comment and a comment the formatter aligned below it in a
  file indented with tabs (`tab_width` other than 4): the next `dotnet format` run moved the separated comment.
- **BRO1132** and **BRO1134** move the `{` up to the header's line when `csharp_new_line_before_open_brace` says so for
  that kind of block (`if (x) // why` + `{`), instead of leaving that to the next `dotnet format` run.
