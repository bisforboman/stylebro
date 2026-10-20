### Fixed

- **`bro format`** no longer stops at the restore when a multi-targeted project has a framework that needs a workload the
  machine doesn't have (`netstandard2.0;net8.0;net8.0-ios` without the iOS workload). It leaves that project alone, as it
  already did for single-target iOS, Android and browser projects, and formats the rest.
