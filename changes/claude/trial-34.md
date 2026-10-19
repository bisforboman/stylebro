### Fixed

- **bro format** settles code where `#if` opens a block in one build only (`#if DEBUG` + `try {` + `#endif` around a
  statement): the Debug and Release runs no longer re-indent the statement inside it on every run, which left `format`
  "still changing after 3 runs" while `bro check` said clean. Its indentation is kept as it is, like the if/else bodies
  `#if` decides.
