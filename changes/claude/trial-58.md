### Fixed

- **`bro check --build`** no longer says "Not clean" over a finding in a project `bro migrate` leaves alone because StyleCop
  never ran there (a test project's xUnit2013, say): `bro format` doesn't touch those projects, so such errors are compared
  with the untouched code's build like any other error.
- **BRO1103** and **BRO1106** no longer report code in an expression tree (an `Expression<...>` lambda or a query over
  `IQueryable`): swapping `'c' == s[0]` or replacing `string.Empty` with `""` there changed the tree a program reads at run
  time.
- **`bro check --build`** no longer says "Not verified" when a project's only build error is CS1685 (a type the compiler
  needs found in two referenced assemblies, with warnings as errors) and the untouched code has it too: the project still
  compiles far enough for StyleBro's rules to run.
