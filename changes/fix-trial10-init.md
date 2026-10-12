### Changed

- **`init` and folders with their own `root = true` `.editorconfig`**: the root's settings never reach such a folder (a
  `sample/` with its own solution), so it now gets a block of its own, judged from its own code and settings, with the
  built-in rule severities too. A folder with too little code to tell keeps StyleBro's defaults, and the report says so.
  `stylebro-migrate --write` writes the whole block into such a folder's `.editorconfig` as well.
- **An `end_of_line` your files contradict** (`init` and `stylebro-migrate --write`): when `.editorconfig` says
  `end_of_line = crlf` but the committed C# files are clearly LF (or the other way round) and git doesn't convert them,
  the settings follow the files for C# files, so `format` no longer flips every file it touches. The report says how to
  switch to the key's ending instead.
- **`init` detects two more conventions**: private static fields with the .NET runtime's `s_`/`t_` prefix (BRO1307 is
  then off instead of renaming them), and files that write `String`/`Int32` instead of `string`/`int` (IDE0049 is then off).
- **`init` and the migration leave out folders your `.editorconfig` marks `generated_code = true`** (`[src/Sdk/**]`)
  when counting conventions and line endings: copied-in code no longer decides your style.

### Fixed

- **`stylebro-migrate --write` with only SA1519/SA1520 on** writes `csharp_prefer_braces = true` instead of
  `when_multiline`: BRO1514 is off there, so BRO1515 and BRO1516 report exactly what StyleCop did. `when_multiline` also
  counted a split `if` header as multi-line and added braces SA1519 accepts. A `csharp_prefer_braces` set at severity
  `silent` or `none` (`when_multiline:none`) is no longer kept as your setting: nothing enforced it.
