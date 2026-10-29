### Changed

- **`bro init`** turns **BRO1503** off when your code puts a blank line below the `{` of its namespaces and types (or
  mixes both), so `bro format` no longer removes those lines from code written that way.

### Fixed

- **BRO1107** and **BRO1108** indent the items of a list whose `(` starts its own line from the name's line when **BRO1109**
  moves the `(` up there, so the result no longer depends on which fix runs first.
