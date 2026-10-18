### Changed

- `bro format` runs a solution's platforms like its configurations: code under
  `'$(Configuration)|$(Platform)' == 'Release|x64'` is fixed and checked too.
- `format` rewrites the indentation of documentation comment lines that `dotnet format` leaves while the build's IDE0055
  reports them (`///` lines indented with tabs under a first line with spaces, at the same column), and
  `--verify-no-changes` reports them.
- The files of NuGet packages a project compiles (source packages such as PolyShim or Nullable) no longer add
  configuration runs or split runs, so the runs no longer depend on whether the repository was restored before.
- A project that a `.slnx` doesn't build (`<Build Project="false" />`) is left alone like one a `.sln` doesn't build.

### Fixed

- `--verify-no-changes` said clean when the Release build of a multi-targeted project with an in-solution source
  generator reported findings: none of them was counted.
- `--verify-no-changes` said clean when that Release build failed (an error in another project, a compile error); it
  now says not clean and shows the first errors.
- `baseline` and `format` write a project's findings to the baseline its build reads, also when `<StyleBroBaseline>` is
  set in a nested `Directory.Build.props`; projects that read different baselines each get their own findings.
- `baseline` run on a solution in a subfolder no longer adds entries for files outside that folder.
- `baseline` with nothing to write names the reason that applies (`none`, or a path it can't resolve).
- On lines whose indentation `#if` decides, `format` now fixes the rest of the line's spacing (`v+1` to `v + 1`) and
  `--verify-no-changes` reports it.
- The CSharpier hint in `--verify-no-changes` says that `format` also runs CSharpier on files with other line endings,
  and the preview names the lines whose indentation `format` keeps.
