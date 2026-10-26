### Fixed

- **Renames (BRO13xx)** and **BRO1409** no longer fail now and then with "The given key '...' was not present in the
  dictionary" in multi-targeted solutions, where a renamed member has an override in another project. This was a
  race inside Roslyn's Find References in the .NET 8 SDK's `dotnet format`. StyleBro now searches again when it
  happens.
