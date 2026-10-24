### Changed

- `bro check --engine host` (experimental) no longer loads the projects again to tell kept findings apart: the copy of the
  repository it formats for that now runs in the workspaces its checks loaded, and reuses what they analyzed. On a
  repository with three configurations (WPF, 15 projects) that took a check from 143 s to 104 s on formatted code and from
  276 s to 209 s before the first `bro format`. The result is the same as before. See
  [The in-process engine](https://bisforboman.github.io/stylebro/format-engine/).
