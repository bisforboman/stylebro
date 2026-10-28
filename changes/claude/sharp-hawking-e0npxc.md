### Fixed

- **`bro check` right after a clean `bro format --json` answers from the format's record again**, as it does after a
  plain `bro format`, instead of running every check (85 s on a medium-sized multi-targeted repository, 0.6 s from the
  record). The `--json` report is the same as before.
