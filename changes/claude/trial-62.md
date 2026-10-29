### Fixed

- `bro init` and `bro migrate` keep the encoding of a UTF-16 (or UTF-32) `Directory.Build.props` or project file they edit,
  and its line endings; they used to write it back as UTF-8 with LF line breaks.
