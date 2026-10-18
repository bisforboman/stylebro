### Fixed

- **`bro format`**: if it's killed while the `#` of protected string lines is swapped for another
  character, the next `bro` command puts the `#` back and says `Restored: <file>`. `format` and
  `--verify-no-changes` stop with a clear message on a swapped character they can't account for.
