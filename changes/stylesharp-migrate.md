### New

- **`stylebro-migrate` reads StyleSharp.Analyzers** (a StyleCop successor with `SST` ids) like StyleCop: its rule
  severities count as the StyleCop rules they stand for (`SST1309` keeps your `_camelCase` fields, `SST1117` your `this.`
  choice), its `stylesharp.*` settings as `stylecop.json`'s, suppressions of SST ids are carried over, and the report says
  where to remove the package and which StyleSharp rules have no StyleBro replacement.

### Changed

- `stylebro-migrate` no longer migrates a leftover `stylecop.json` (no StyleCop package referenced) when the repository
  has a Sonar setup: it points to `stylebro-migrate init`, which follows the Sonar setup and the code's own conventions.

### Fixed

- `stylebro-migrate --write` gives a `root = true` folder that turns every analyzer off the same opt-out block as `init`,
  instead of turning rules on there.
- `stylebro-migrate` says when the repository's own `dotnet_sort_system_directives_first` doesn't match the order of
  its using directives; when the team's ordering rule (SA1210, or StyleSharp's case-sensitive SST1210) accepts that
  order, `format` leaves those files alone instead of sorting them into what that rule reports.
- `stylebro-migrate --write` no longer adds suppressions for rules its settings turn off (StyleSharp's SST1462 reported
  them).
- Code in a folder without a project of its own (shared polyfills linked into the projects) gets the migrated settings
  of the projects that run StyleCop, instead of StyleBro's defaults.
- `stylebro-migrate format` no longer says "another formatter's code" where IDE0055 is just off, and no longer asks to
  rebuild a generated file only a target framework it didn't build compiles.
