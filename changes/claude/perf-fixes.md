### Changed

- `bro format` and `bro check` list IDE fixes they hold back on an older .NET SDK (IDE0049 and the like, where that SDK's
  `dotnet format` ignores StyleBro's multi-target guard) as a note: on their own they no longer make the result
  "Not verified" (exit code 3). Projects or frameworks that didn't load, skipped configurations and excluded files still do.

### Fixed

- `dotnet format` runs about a third faster on large repositories with `#region`s: removing regions (BRO1112) scanned the
  whole solution again for every file it fixed (Newtonsoft.Json: that one rule's fixes took 387 s, now about 15 s).
