### Changed

- `bro format` leaves files a project compiles from outside the repository alone, like git submodules
  (`Leaving alone: ../outside/Shared.cs (outside the repository)`); before, it rewrote them without listing them.
- Where CSharpier formats the repository and `end_of_line` is `lf` or `crlf`, `format` also runs CSharpier on the C#
  files with other line endings, so `dotnet csharpier check` passes afterwards. `--verify-no-changes` also counts
  CSharpier's findings in Directory.Build.props and Directory.Packages.props.
- `--verify-no-changes` builds a project with an in-solution source generator in its other configurations (Release),
  which `format` can't run there, and is not clean when that build reports one of the rules `format` fixes.
- A `format` run that changed only line endings or byte order marks is the last one: no second run checks every
  framework again.

### Fixed

- `format` no longer goes back and forth on lines whose if/else or loop body `#if` decides
  (`#if NETFRAMEWORK` ... `else` / `#endif` / `builder.Append(v);`): each framework's or configuration's run indented
  them its own way, so it never settled. It now leaves their indentation alone and says so (`Indentation kept: ...`).
- Where CSharpier puts back what a fix changed, `format` and `--verify-no-changes` name the rules whose edits it undoes,
  and `format` no longer says "clean" right before "Not clean". The preview (`init --diff`) says not clean and exits 2
  there too, like `format`.
- `format` no longer says "style and analyzer fixes still run there" about a folder it leaves alone.
- The hint about warnings loading the workspace names the run that had them and the command that shows them
  (`--verbosity diagnostic` on the same `bro format` command).
- `format <solution in a subfolder> --files <file elsewhere in the repository>` no longer says the file "isn't under"
  the solution's folder.
- `--verify-no-changes` with CSharpier runs its CSharpier check after StyleBro's, not before the first line.
