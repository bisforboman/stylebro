### Fixed

- **bro format** settles code where `#if` opens a block in one build only (`#if DEBUG` + `try {` + `#endif` around a
  statement): the Debug and Release runs no longer re-indent the statement inside it on every run, which left `format`
  "still changing after 3 runs" while `bro check` said clean. Its indentation is kept as it is, like the if/else bodies
  `#if` decides.
- **bro format** no longer ends with "Clean" when its last check found a warning that no fix changes: it lists it and
  says "Not clean", like `bro check` (the build reports such a warning).
- **BRO1008** no longer reports usings in a file where a `#if` branch the build leaves out declares a namespace, or a
  type outside the namespace (a polyfill for older frameworks): the fix needs every build of the file to agree, so the
  warning used to stay for good.
