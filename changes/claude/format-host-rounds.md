### Changed

- `bro format --engine host` (experimental) fixes StyleBro's rules whose fixes don't meet together: one analysis for all of
  them, where it analyzed again before every rule. Rules whose fixes do meet keep their order, renames come last, and a
  fix that touches another's edit runs on its own. The result is the same as before. See
  [The in-process engine](https://bisforboman.github.io/stylebro/format-engine/).
