### Changed

- **`bro init`** turns **BRO1303** off when your code names its private fields with two or more words in snake_case
  (`break_target`), or mixes snake_case and camelCase, so `bro format` no longer renames them.

### Fixed

- `bro init` and `bro migrate` keep the encoding of a UTF-16 (or UTF-32) `Directory.Build.props` or project file they edit,
  and its line endings; they used to write it back as UTF-8 with LF line breaks.
