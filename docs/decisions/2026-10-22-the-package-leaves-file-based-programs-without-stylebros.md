# The package leaves file-based programs without StyleBro's analyzers (2026-10-22)

## Question

Trial 42, PolyShim: its build runs `GenerateSignatures.csx` with `dotnet run` (a .NET 10 file-based program). The program
imports the repository's Directory.Build.props, StyleBro's PackageReference and TreatWarningsAsErrors included, so after
`bro init` and `bro format` the build failed on 37 BRO findings in that file. `bro format` never touches it (it is in no
project, and `dotnet format` 10.0.113 can't load file-based programs), format and check didn't name it, and only
`bro check --build` caught it. A developer running `dotnet run script.cs` in such a repository hits the same errors.

## Choices

1. **Package skips them** (owner's pick): the package's targets remove its analyzers where the SDK sets
   `FileBasedProgram=true`, so the build reports nothing `format` can't fix (like submodules and vendored code).
2. `bro format` formats each file-based program through a temporary project compiling the file with the repository's settings.
3. Keep the analyzers; format and check name file-based programs as "Not checked" (exit 3).

## Decision

Package skips them: `_StyleBroLeaveFileBasedPrograms` (after ResolveLockFileAnalyzers) removes the `Analyzer` items of
the StyleBro.Analyzers package when `FileBasedProgram` is true. The scripts stay as they are.
