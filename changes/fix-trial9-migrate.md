### Changed

- **Line endings that git's checkout contradicts** (`stylebro-migrate --write` and `init`): when your `.editorconfig` sets
  `end_of_line` but git gives C# files each machine's ending on checkout (`*.cs text=auto`, or `core.autocrlf`), the
  settings keep endings out of it: `end_of_line = unset` for C# files and IDE0055 as a suggestion, so a Windows checkout
  neither fails the build on line endings nor gets rewritten. The report says how to settle it (`*.cs text eol=lf` in
  `.gitattributes`, or removing `end_of_line`).
- **`init` detects grouped list items:** argument and parameter lists whose items are grouped on lines
  (`F(tag, 1,` then `"a", "b")`) turn BRO1108 off, as for the other conventions.
- **BRO1122** writes unsigned suffixes in lower case (`6u`, `1ul`) when BRO1135 is off.

### Fixed

- **`stylebro-migrate --write`** no longer turns on StyleCop rules that are off by default (SA1305, SA1412, ...) because
  a category-wide or repository-wide severity is set (`dotnet_analyzer_diagnostic.category-StyleCop.CSharp.NamingRules.severity`):
  like in the compiler, those only apply to rules that are on by default. BRO1310 renamed `spCount` to `count` before.
- **A second `--write` after removing StyleCop.Analyzers** keeps the rules the old StyleCop version didn't have off: the
  version is recorded in the generated block. The closing "Next:" line now follows where the package swap stands.
- **Sonar rules follow the referenced SonarAnalyzer.CSharp version** (also from central package versions and
  packages.config): a rule that version doesn't have (S8969 before 10.30) or doesn't turn on by default (S2325 before 9.32)
  no longer turns on the rule that fixes it.
- **BRO1604** no longer asks for "a value indicating whether" on `bool?` properties, like StyleCop.
