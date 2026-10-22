### Fixed

- `bro init`, `bro migrate`, `bro format`, `bro check` and `bro preview` also recognize a NuGet package that copies its
  `.editorconfig` over the root one when the repository's committed copy is an older version of the package's file: a
  package whose build files name that file counts. Before, only an exact copy was recognized, so they said "Clean" and the
  next build reported what the overwritten settings had turned off.
- `bro init` no longer writes `</PropertyGroup></Project>` on one line into a `Directory.Build.props` that was
  `<Project></Project>`: CSharpier's check rejected the file, so `bro check` said "Not clean" right after a clean `bro format`.
