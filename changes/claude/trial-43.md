### Fixed

- **bro init** sets StyleBro's rules by id when a `.globalconfig` turns analyzers down in bulk
  (`dotnet_analyzer_diagnostic.severity = none`, or a category below warning): `dotnet format` then skipped them, so
  `bro format` fixed nothing and `bro check` said "Clean" while the build reported them. `bro format` and `bro check` now
  say "Not clean" for such a setup and point to `bro init --write`.
- **bro init** and **bro migrate** say when git ignores the `.editorconfig` they write (a `.editorconfig` line in
  `.gitignore`), with the line and how to commit the file; **bro preview**'s patch now includes such a file.
- **BRO1606** leaves a constructor summary alone when the type's declaration has `#if` branches (a generic class in one
  build, a plain one in another): a file two projects compile got the other class name on every `bro format` run.
- **BRO1603-BRO1607** leave documentation text that is a string, or is in a snapshot file, in the solution (a test checking
  what NSwag or Swashbuckle reads from XML docs into an OpenAPI description): the period or standard words broke that
  test. The finding is kept, with the reason, like a kept rename.
