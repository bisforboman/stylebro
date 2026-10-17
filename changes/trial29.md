### Changed

- **`stylebro-migrate init`** counts how your public and internal fields are named when no naming rule decides it: code
  that names them in camelCase turns BRO1306 off instead of having them renamed to PascalCase.

### Fixed

- **BRO1302** no longer renames parameters Harmony binds by name: a name starting with `__` (`__instance`, `__result`,
  `___field`) and every parameter of a Harmony patch method stay as they are. Renamed, they compiled and the patches
  stopped working.
- **`stylebro-migrate format`** formats the hand-written `.cs` file next to a T4 template that writes another file
  (`<#@ output extension=".Generated.cs" #>`); it used to skip it, and the build then reported its findings.
