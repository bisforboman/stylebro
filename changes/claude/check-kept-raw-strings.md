### Fixed

- **`bro check` now says what `bro format` said about a file it left alone.** When a fix or the whitespace formatting would
  change the text of a raw string literal, `bro format` keeps that file as it was and reports it as `Not checked:`. `bro check`
  (also from the fast clean record, and with `--json`) used to say "Clean" for the same files while the build still failed on
  them; it now prints the same `Not checked:` line and ends with `Not verified` (exit code 3).
