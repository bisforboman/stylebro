### New

- **`stylebro-migrate format`** (and `baseline`, `--diff`) runs on a repository whose root build is a Traversal project
  (`Sdk="Microsoft.Build.Traversal"`), through a temporary solution with the projects it lists.
- With no solution or project at the root, the commands use the only solution one folder down (`src/MySolution.slnx`)
  and say so.

### Fixed

- **`stylebro-migrate format`** no longer crashes when two projects compile the same files (a library and its AOT
  variant compiling `..\Lib\*.cs`): such projects are formatted in separate runs. When `dotnet format` does crash, the
  command names the run's projects and lists every file the run had already changed.
- **`stylebro-migrate format --diff`** no longer fails its restore (NU1008) when a project turns on central package
  management in its own project file.
- **`stylebro-migrate init`** without `--write` on an already migrated repository says the settings are there, as
  `--write` does, instead of showing a block to add.
- **`stylebro-migrate`** keeps the line endings of the `.editorconfig`, props and project files it edits (a CRLF
  `.editorconfig` came back LF).
