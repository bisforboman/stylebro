### Fixed

- `bro init`, `bro migrate` and `bro preview` no longer skip a folder that holds a committed `.nupkg` next to a solution or
  project (DbUp's `src/`): they took it for an extracted package, so init judged the conventions from no code and put the
  preset version, and the preview the package reference, into a new root `Directory.Build.props` the projects never read.
- `bro init` and `bro migrate` say when the repository's own `end_of_line` makes `bro format` convert files committed
  with the other line ending (each a whole-file change), and how to keep that out of the formatting changes.
