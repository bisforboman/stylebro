### Changed

- **BRO1120** leaves a file's header alone: the comments a file starts with, also right below a leading `#region` line,
  keep their empty `//` lines (a license header's first and last line). `bro init` no longer turns BRO1120 off for such
  headers.

### Fixed

- **BRO1513** adds BRO1505's blank line too where a `#region` sits right below a member's `}` and above a doc comment:
  `bro format` no longer leaves a blank line above the `#region` or not depending on which rule it fixes first.
