### Fixed

- **stylebro-migrate** follows Sonar severities per folder: a sub-directory `.editorconfig` or a path section (like
  `[tests/**.cs]`) that lowers or turns off a Sonar rule now lowers or turns off the StyleBro rule that fixes it there,
  instead of turning that rule on at warning everywhere. `init` writes these as sections of its block.
- **stylebro-migrate** reads `.editorconfig` sections that list several paths in braces
  (`[{src/tests/**/*.cs,tests/**/*.cs}]`); their StyleCop severities were ignored before.
