### Changed

- `bro format`, `bro check` and the previews now run their checks and fixes inside the `bro` process by default: each
  configuration's solution is loaded once for the whole command instead of once per `dotnet format` run, which made them
  1.6 to 3.3 times faster on the six repositories it was measured on, with the same results. To get the old engine back (one
  `dotnet format` process per run), add `--engine dotnet-format`; it stays available for at least one release. Where the
  in-process engine can't run (for example a `global.json` that selects a .NET 8 SDK), the command says so in an `Engine:`
  line and uses `dotnet format` as before.
