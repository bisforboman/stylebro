### Changed (stylebro-migrate only)

- A folder with one solution and several solution filters of it (`.slnf`) uses the solution; the commands say how to
  name a filter instead. A single filter next to a solution is still a choice you make.
- A failed restore with NU1102 for the StyleBro packages explains how to add the package source and its
  `packageSourceMapping` entry.
- `init` and the migration name CI steps that run plain `dotnet format --verify-no-changes` (GitHub workflows, Azure
  Pipelines): findings StyleBro keeps on purpose fail them, `stylebro-migrate format --verify-no-changes` doesn't.

### Fixed (stylebro-migrate only)

- **`format` on a solution in a subfolder** (`stylebro-migrate format src/App.slnx`) now leaves alone the folders the
  migration marked "No StyleCop ran in": it read only the solution folder's `.editorconfig`, so vendored code was
  reformatted.
- **Package versions from MSBuild properties** (`Version="$(StyleCopVersion)"`) are read from the repository's props
  files, for StyleCop.Analyzers and SonarAnalyzer.CSharp. One that no file defines is reported as unknown instead of
  being written into the settings as `$(StyleCopVersion)`.
- **`baseline` takes a solution or project first**, like `format` (`stylebro-migrate baseline src/App.csproj`), and no
  longer says it picked the project you named with `--project`.
- **The preview** no longer warns "StyleBro reported nothing" on code that already follows every rule: whether the
  analyzers load is checked in the restore output.
