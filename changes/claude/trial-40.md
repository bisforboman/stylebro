### Fixed

- **BRO1102** splits a list that shares its line with code into `[A][B]`, without a space: the formatter removed the space
  on the next `dotnet format` run.
- **`bro migrate --write` and `bro init --write`** write their settings into an `.editorconfig` in each top-level folder
  with projects when the root `.editorconfig` is a copy of a git submodule's that the build puts back (SixLabors'
  shared infrastructure). Before, the first build removed them, and the build reported findings `bro check` had called
  clean.
