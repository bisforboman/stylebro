### Changed

- **A faster first `stylebro-migrate` run:** projects are evaluated in parallel and once, only the first run restores,
  and the clean-check run checks every target framework at once. `format` and `init --diff` take 35-60 % less time on
  the repositories we tried (395 s to 157 s on a 99-project solution), with the same results.

### Fixed

- **Renames in multi-targeted repositories:** `format` runs each target framework with only the projects that target
  it, so a rename couldn't see the other projects' code. Now it does: a field a test reads with `GetField("count")`
  keeps its name, and a name used in a project that isn't loaded is kept with that reason.
- Spellings of one target framework (`net6`, `net60`, `net6.0`) share one run.
- Messages: a failed preview gives its reason first; `init` says to add the package when nothing references it yet;
  `--verify-no-changes` says it changes nothing; `--once` lists the kept findings.
