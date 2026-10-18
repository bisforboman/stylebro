### Fixed

- **`bro baseline`** no longer hangs when `dotnet format` reports a lot of whitespace changes (it waited
  forever on a large multi-targeted solution).
- **`bro baseline`** writes the baseline the build reads: the file `<StyleBroBaseline>` names (with `../`
  paths when it's outside the code, e.g. `eng/stylebro.baseline`), else the nearest `stylebro.baseline` above, and says
  where it wrote. With `<StyleBroBaseline>none</StyleBroBaseline>` it writes nothing. Entries for files outside the
  folder it ran on stay.
