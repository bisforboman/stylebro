### Fixed

- **BRO1519** no longer asks for a blank line above a `checked`/`unchecked` statement when CSharpier formats the
  repository: CSharpier always removes that line, so the two formatters undid each other and `bro check` stayed Not clean.
