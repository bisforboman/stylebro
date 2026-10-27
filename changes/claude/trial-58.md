### Fixed

- **`bro check --build`** no longer says "Not clean" over a finding in a project `bro migrate` leaves alone because StyleCop
  never ran there (a test project's xUnit2013, say): `bro format` doesn't touch those projects, so such errors are compared
  with the untouched code's build like any other error.
