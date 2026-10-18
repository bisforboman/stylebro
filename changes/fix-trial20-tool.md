### Fixed

- **`bro baseline`** records the right lines when `dotnet format`'s whitespace pass would split lines
  (`if (x) y;`): it reads each pass on its own, so the build is clean after a baseline.
- **`bro format`** on a solution in its own folder (`solutions/App.slnx` listing `../src/...`) formats the
  projects instead of reporting "clean" without touching them; a path that doesn't exist says "Not found".
- **`bro format`** removes usings its fixes leave unused (IDE0005) where the repository enforces IDE0005.
- **`--diff`** previews show changes to files git tracks under an ignored folder, and say when an ignored `nuget.config`
  isn't in the copy.

### New

- **`bro format`** names checked-in generated files in the projects it changed ("rebuild and commit it"),
  and reminds you to run CSharpier where the repository uses it.
