### Fixed

- `bro format` and `bro check` no longer say "Not checked ... The target "CoreCompile" does not exist" for a project that lists a single framework in `<TargetFrameworks>` (say, a `Directory.Build.targets` that drops `net48` off Windows) when MSBuild logs a warning while loading it; the project was checked, and the false "Not verified" (exit 3) came with it.
- A fix that would change the text of a raw string literal (`"""..."""`) is dropped and the file stays as it was, with a warning naming it. Before, IDE0055's formatter could add a space to the lines of a `$$"""` literal in every run, so `bro format` never settled and the program's text changed.
