### Changed

- `bro format --engine host` and `bro check --engine host` (experimental) reuse more of their analysis: a fix run starts
  from the check before it, and the check after a run that changed files analyzes again only the projects a fix reached.
  A command's later runs no longer load the projects again either. The result is the same as before. See
  [The in-process engine](https://bisforboman.github.io/stylebro/format-engine/).
