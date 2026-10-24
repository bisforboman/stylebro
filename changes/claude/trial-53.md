### Fixed

- `bro format` no longer misses changes to C# files in a folder that holds a committed `.nupkg` next to code (a test's
  `TestData/` folder): it took the folder for an extracted package and reported "0 files changed" after fixing them.
  `bro init` and `bro preview` read such folders too now.
- `bro init` and `bro migrate` say when the repository's own `charset` or `insert_final_newline` makes `bro format`
  change some files (a BOM removed or added, a last line break added or removed), and how to keep that out of the
  formatting changes.
