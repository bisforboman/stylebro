### Changed

- `stylebro-migrate format` fixes the Release-only code (`#if !DEBUG`) of projects that use an in-solution source
  generator: it builds the generator in Release first and gives the project a Release run like any other project.
- A project whose project file picks its `Compile` items by `$(Configuration)` (a file only Release compiles) gets a run
  in that configuration too, so that file is formatted and checked.
- `--verify-no-changes` counts the documentation comment indentation `format` rewrites only in projects without
  `GenerateDocumentationFile`, where the build reports it; with documentation generated the build reports none or only
  some of those lines.
- When `dotnet format` had warnings loading the workspace, `format` names the restore's first warning (such as NU1603)
  instead of guessing at the cause.
- `--diff` and `format` put a new `stylebro.baseline` in the same place: next to the projects of the solution `format`
  runs on, also when run on the repository folder.

### Fixed

- Old-style projects (`TargetFrameworkVersion`) in a solution with multi-targeted projects got no `format` run, and
  `--verify-no-changes` said clean without checking them.
- When `dotnet format` couldn't load a project for the framework whose run also stood in for another one (netstandard1.3
  for netstandard2.0), neither was checked; the other framework now runs instead. Code that no run could load makes
  `--verify-no-changes` say `Not verified` instead of clean.
- `--verify-no-changes` said clean where `end_of_line` asks for another line ending than a file has; `format` converts
  those files.
- `--diff` showed every file `format` changed with Windows line endings in a repository with LF files.
- `--verify-no-changes` showed a failed Release build only when that build had no findings; it now shows the failure
  next to the findings and says baselining them doesn't fix it.
- `baseline`'s "Not in the baseline: N formatting changes" says that the baselined findings on those lines show up again.
