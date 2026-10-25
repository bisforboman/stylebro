### Fixed

- `bro check` no longer lists `IDE0055` next to a whitespace finding at the same place, or other findings a second time at
  their positions before whitespace formatting, in a multi-targeted repository where a project without one of the
  frameworks references another multi-targeted project: it lists what `--engine dotnet-format` does. `bro format --json`
  counts those fixes once too.
