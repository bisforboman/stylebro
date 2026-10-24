### Fixed

- **BRO1309** no longer renames a static method to `Main` (`__Main` -> `Main`): any static `Main` can be an entry point, so
  the rename made a second one (CS0017) in projects that already have one, such as test projects on .NET Core.
- `bro format` and `bro check` no longer crash in every run when one project has a file as an additional file
  (`AdditionalFiles`, as some source generators add them) and another project compiles it: those projects get runs of their
  own, and a project that loads both runs on its project file.
- `--engine host` falls back to `dotnet format` when it can't load the solution (an `Engine:` line says why), instead of
  failing every run. `bro check` now shows that line too.
- `bro check` no longer says "Not verified ... the rest is clean" when one run failed after other runs found something: a
  failed run decides the exit code.
- `bro preview` takes `--engine` (`bro preview init --engine host`, `bro preview format --engine host`, and
  `bro format --diff --engine ...`): before, `init` called it an unknown option, and `format` failed with "The preview
  failed". The preview's format runs now use that engine, as `bro format` does.
- A preview of a subfolder (`bro preview format Lib`) no longer adds `Lib/Directory.Build.props` when a
  Directory.Build.props above it already references StyleBro.Analyzers.
