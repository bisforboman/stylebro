### New

- **CSharpier after `format`**: where the repository formats with CSharpier (its `csharpier` tool, or the
  CSharpier.MsBuild package), `bro format` runs it on the C# files each run changed, so one command leaves
  a tree `dotnet csharpier check` accepts. `--verify-no-changes` runs CSharpier's check on the C# files it formats too.
  Where CSharpier puts back what a fix changed (a modifier order), both say so and name the files.

### Fixed

- `format` no longer breaks an interpolated raw string (`$"""`, `$$"""`) whose first line starts with `#` inside an `#if`
  branch (CS8999 in the build that compiles the branch): every string shape is protected now.
- A CR without an LF inside a string (a verbatim regex) no longer becomes a line break where `.editorconfig` sets
  `end_of_line`: `format` leaves such files out of the whitespace and style passes and says so.
- Files a project compiles only for some target frameworks (`<Compile Include="..\Polyfills\*.cs">` under a framework
  condition) are formatted and checked now; `--verify-no-changes` said clean while the build failed on them.
- StyleBro rules listed in `<WarningsNotAsErrors>` are fixed and checked now (plain `dotnet format` skips them while the
  build reports them as warnings).
- `format` run from the repository's root removes unused usings where only a folder's `.editorconfig` enforces IDE0005.
- A baseline named with `<StyleBroBaseline>` outside the code (`eng/stylebro.baseline`) now gets the findings `format`
  keeps, and covers them in the build (`../src/...` entries). With `<StyleBroBaseline>none</StyleBroBaseline>`, `format`
  and `--verify-no-changes` say that the build still reports the kept findings. The baseline message counts entries.
- EF Core migration designers (`*.Designer.cs` next to a migration) are no longer listed as generated files to rebuild.
- `init` (and `init --diff`) no longer spends minutes evaluating projects: reading `dotnet msbuild`'s output waited for
  thread-pool threads. `init --diff` says it can't choose a solution before evaluating anything.
- `format` with several runs (multi-targeted projects) checks them all at once first, also in the first run, and formats
  only the runs with findings: a repository with nothing to fix no longer waits for one `dotnet format` after another.
- `format --files` lists the changed files relative to the repository, not the project.
