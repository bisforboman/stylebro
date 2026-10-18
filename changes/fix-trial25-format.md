### New

- **Release code is fixed and checked too**: where a project's code tests `DEBUG`, `RELEASE`, `TRACE` or its own
  configuration symbols, `bro format`, `--verify-no-changes`, the preview and `baseline` also run its other
  configurations (Release), only where the code compiles differently there. Before, code under `#if !DEBUG` was never
  checked and a Release build with warnings as errors failed after a clean check. A project that runs an in-solution
  source generator stays in its default configuration, and `format` says so.

### Changed

- `format --files` with files in several projects runs once for all of them (two edited files in two of 11 projects:
  27 s instead of 39 s), and a framework whose check found something formats just those files. `--verify-no-changes --files` formats a
  copy of just the files with findings.
- CSharpier after `format` and in `--verify-no-changes` gets whole folders where it would change nothing else, and says
  when it runs: a check of 612 files took 2 s instead of 85 s.
- The preview's patch includes the baseline entries for the findings kept on purpose, and its summary lists them.
- A killed `format` no longer leaves `dotnet format` running on Windows, and old temporary files of killed runs are
  cleaned up.
- The progress lines name what a run loads (`Checked 11 projects (run 1 of 1)`), and the line about folders the
  whitespace pass leaves out starts with `Whitespace pass leaves out:`.

### Fixed

- A relative `<StyleBroBaseline>eng/stylebro.baseline</StyleBroBaseline>` is found by the build now (it looked in each
  project's folder, so `format` said clean and the build failed). The docs recommend `$(MSBuildThisFileDirectory)`.
- The preview no longer removes `using` directives code needs when a project compiles files from outside the
  repository: the copy gets those files. Where they're missing, `format` says so and leaves IDE0005 out.
- The note about generated files checked in no longer names a `Settings.Designer.cs` (Visual Studio writes it) or a
  `.g.cs` file no build step writes.
