### Fixed

- `bro preview format --engine host` (and `bro format --diff --engine ...`) no longer fails with "The preview failed":
  the preview takes `--engine` and formats its copy with that engine, as `bro format` does.
