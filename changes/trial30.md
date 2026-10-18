### Fixed

- **`bro format`** (and `--verify-no-changes`, `--diff`) no longer stops when a project in the solution needs
  a workload that isn't installed, such as an Android, iOS or Browser head (`dotnet restore` failed with NETSDK1147 and
  nothing was formatted). Those projects are listed under "Leaving alone" and the rest of the solution is formatted; run
  `dotnet workload restore` and `format` again to format them too.
