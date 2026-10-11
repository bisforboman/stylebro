### Changed

- **Faster `dotnet format`:** analyzers that only look at syntax no longer make the compiler bind every method body
  first, and Fix All works on documents and renames in parallel. A full run on Newtonsoft.Json takes about half the
  time (its private field renames: 158 s to 41 s), with the same results.
