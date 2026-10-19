### Changed

- **bro format** converts a C# file's old Mac line endings (a CR alone, outside string literals) to the file's own line
  break before formatting it, and says which files. It used to leave such files out, end with "Clean", and the build then
  reported StyleBro's findings there. `bro check` reports them as files formatting would change.
- **bro format** leaves xUnit's assertion fixes out where the restore output has xunit.analyzers 1.24 or later (as with
  `xunit.v3` 3.x), and says why: from that version on its fixes don't support Fix All, which `dotnet format` needs. It used to
  say "Clean" while `bro check` reported the same warnings as not fixed.

### Fixed

- **BRO1405** moves the `let`/`where`/`select` lines of a multi-line query along when it removes the parentheses in front of
  its `from`, so `bro format` needs one run instead of two.
