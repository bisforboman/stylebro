### New

- `bro format --engine host` (experimental) now applies the fixes inside the `bro` process too, not only the checks: each
  configuration's projects are loaded once for the whole command, the rules are fixed in a fixed order, and between two
  rules only the projects a fix changed are analyzed again. The result is the same as with `dotnet format`; on the
  trial repositories `bro format` took 29-64 % less time. See
  [The in-process engine](https://bisforboman.github.io/stylebro/format-engine/).
