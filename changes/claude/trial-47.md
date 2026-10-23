### Fixed

- **BRO1107**, **BRO1108**, **BRO1110** and **BRO1111** indent the lines they create with spaces in a file whose
  `.editorconfig` says `indent_style = space`, even when the line they copy the indentation from starts with tabs. A
  second `bro format` run no longer re-indents those lines.
- `bro format --json` no longer prints "Couldn't delete the copy" after a run whose restore failed.
