### Fixed

- **`bro check --build`** no longer says "Not clean" over a finding in a project `bro migrate` leaves alone because StyleCop
  never ran there (a test project's xUnit2013, say): `bro format` doesn't touch those projects, so such errors are compared
  with the untouched code's build like any other error.
- **BRO1103** and **BRO1106** no longer report code in an expression tree (an `Expression<...>` lambda or a query over
  `IQueryable`): swapping `'c' == s[0]` or replacing `string.Empty` with `""` there changed the tree a program reads at run
  time.
