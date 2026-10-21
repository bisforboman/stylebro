### Fixed

- **BRO1102** splits a list that shares its line with code into `[A][B]`, without a space: the formatter removed the space
  on the next `dotnet format` run.
