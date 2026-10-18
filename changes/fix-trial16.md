### Changed

- **bro init** turns off **BRO1102** when the code mixes `[A, B]` and one attribute per list, or mostly
  combines them.
- **bro init** says why a few one-line jumps (`if (x) return;`) are enough to keep them allowed.

### Fixed

- **bro init** and **--write** find CSharpier when its tool manifest or `.csharpierrc` sits next to the
  solution or a project rather than at the repository root. When CSharpier formats only one folder, its settings go into
  that folder's section.
- With CSharpier, **bro init** and **--write** no longer write a using sort key: CSharpier sorts usings
  itself, and `dotnet format` ordered some of them differently.
- Files **bro** creates (such as `Directory.Build.props`) get the line endings of the files around them.
- **--diff** adds StyleBro's package version where central package management is turned on by a `Directory.Packages.props`
  below the root, and never into a conditional `ItemGroup`.
- **--diff** leaves packages.config projects out and previews the others, instead of refusing the whole repository.
