### Fixed

- **BRO1303** gives the same result in every `dotnet format` run: in a project with several target frameworks, a renamed
  static field that a local hides was written as `Namespace.Type.field` in some runs and `Type.field` in others. It's
  now always the shortest name that still means the field.
- The renaming rules (BRO1301-BRO1314) are faster under `dotnet format`: each rename's safety check runs once instead of
  twice.
