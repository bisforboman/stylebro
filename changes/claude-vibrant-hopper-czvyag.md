### New

- **`stylebro-migrate format --timings`** prints how long each phase took (evaluating the projects, the restore, each
  check and fix run, CSharpier, the builds `--verify-no-changes` makes).
- **`stylebro-migrate format --restore`** restores even when the restore output looks current (see below).

### Changed

- **`stylebro-migrate format` is faster before the first `dotnet format` run.** It skips `dotnet restore` when every
  project's restore output lists StyleBro.Analyzers, its packages are there and nothing it depends on changed since
  (project, props, targets, `nuget.config` and `global.json` files); evaluating the projects for other configurations
  runs all at once instead of partly one after another.
- **`--verify-no-changes` right after a clean `format` answers in seconds.** When the code, settings, options, SDK and
  StyleBro versions are the ones that `format` ended clean on, and its last run checked every target framework and
  configuration, verify answers from that record instead of running every check again. The builds of the
  configurations an in-solution source generator keeps from the checks still run, now one build per configuration for
  all its projects.
