### Changed

- **BRO1120** leaves a file's header alone: the comments a file starts with, also right below a leading `#region` line,
  keep their empty `//` lines (a license header's first and last line). `bro init` no longer turns BRO1120 off for such
  headers.
- `bro format`, `bro check` and `bro preview` name a git submodule that isn't checked out (`Missing: ...`, with
  `git submodule update --init`) and leave out IDE0005 there, as they do for compiled files that are missing: code that
  compiles files from it doesn't compile, so `using` directives can look unused.

### Fixed

- **BRO1513** adds BRO1505's blank line too where a `#region` sits right below a member's `}` and above a doc comment:
  `bro format` no longer leaves a blank line above the `#region` or not depending on which rule it fixes first.
