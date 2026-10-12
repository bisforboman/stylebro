### Fixed

- **`stylebro-migrate format` gives the same result on every OS** when `.editorconfig` sets no `end_of_line`: the
  whitespace pass wrote the OS's line ending into the lines it rewrote (after `=>` followed by a comment, for example),
  so CRLF files got LF lines on Linux and LF files CRLF lines on Windows, and `--verify-no-changes` was clean on one OS
  and failed on the other. A file that uses one line ending throughout keeps it, and `--verify-no-changes` ignores
  findings that only swap it.
