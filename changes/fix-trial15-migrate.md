### Fixed

- **stylebro-migrate --write** asks MSBuild which projects run StyleCop whenever a repository has several projects, so an
  import the tool can't read (like `GetPathOfFileAbove($(MSBuildThisFile), $(MSBuildThisFileDirectory)..)` in
  Library.Template repositories) no longer makes a folder look StyleCop-free. Such a folder got no settings before and
  showed hundreds of StyleBro warnings that contradicted StyleCop. The unquoted `GetPathOfFileAbove` form is now read too.
- The report line for folders without StyleCop now says what really happens there (StyleBro's rules still report in the
  build; remove the block's `# No StyleCop ran in` line to format them) instead of pointing to `init`.
- **stylebro-migrate init --write** run again gives a byte-identical .editorconfig: the using-order comment without a
  setting line was written a second time.
- **stylebro-migrate format --verify-no-changes** counts only files formatting really changes: not files a build step
  generates during the check, and not files whose only change is a line ending.
- The preview no longer puts the `StyleBro.Analyzers` PackageVersion between a comment and the item it describes.
- `init` names the Directory.Build.props the projects import in its `<EnforceCodeStyleInBuild>` hint
  (`src/Directory.Build.props` when that's the only one).
