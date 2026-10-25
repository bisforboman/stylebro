### Fixed

- **`bro init`** counts a rule once in its summary when two conventions turn it off: "Turned 12 rules off ... BRO1306
  (non-private fields; private constants/static readonly fields)" instead of listing BRO1306 twice and counting 13.
- **`bro format`** no longer tells you to rebuild and commit a LINQ to SQL, DataSet or Entity Framework 6 designer's
  `.designer.cs` (next to its `.dbml`, `.xsd` or `.edmx`): Visual Studio's designer writes it, not the build.
- **`bro format`** loads a library that lacks the framework of the run in progress, when one of its packages has build props
  and targets (ReferenceTrimmer, for example). Such a library failed to load, and so did every project that references it:
  those projects were left unformatted with "Not verified", and `bro check --build` then found what format had missed.
