### Fixed

- **BRO1107**, **BRO1108**, **BRO1110** and **BRO1111** indent the lines they create with spaces in a file whose
  `.editorconfig` says `indent_style = space`, even when the line they copy the indentation from starts with tabs. A
  second `bro format` run no longer re-indents those lines.
- `bro format --json` no longer prints "Couldn't delete the copy" after a run whose restore failed.
- `bro format` and `bro check` also run the Release configuration (or another one) of a project that generates its
  documentation file only there: BRO1601's findings in it were left for the Release build, and `bro check` said clean.
- `bro format --engine host` and `bro check --engine host` no longer report findings twice, the second time at the
  positions before whitespace formatting, in a framework's run whose projects referenced another framework of a project
  that run checks: they report what the default engine does.
