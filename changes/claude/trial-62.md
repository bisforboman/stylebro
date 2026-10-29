### Changed

- **`bro init`** turns **BRO1308** off when your code names its private fields with two or more words in snake_case
  (`break_target`), or mixes snake_case and camelCase, so `bro format` no longer renames them.
- **`bro init`** keeps tab indentation: when your C# files are indented with tabs, it writes `indent_style = tab`, so the
  lines `bro format` adds or re-indents get tabs too instead of spaces.
- **`bro init`** turns **BRO1120** off when your code frames comments with empty `//` lines (`//` / `// text` / `//`), or
  mixes framed and plain comments, so `bro format` no longer removes those lines.

### Fixed

- `bro init` and `bro migrate` keep the encoding of a UTF-16 (or UTF-32) `Directory.Build.props` or project file they edit,
  and its line endings; they used to write it back as UTF-8 with LF line breaks.
