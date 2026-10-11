### Fixed

- **packages.config projects:** StyleBro.Analyzers goes in the packages.config way there (the packages.config entry, the
  `<Analyzer>` items and the build targets import, as NuGet writes them), and a `PackageReference` in
  `Directory.Build.props` gets `Condition="!Exists('$(MSBuildProjectDirectory)\packages.config')"`. A
  `PackageReference` switched such projects to PackageReference restore, so a fresh clone didn't restore their other
  packages and didn't build. The migration's report says so, and the old-style section of the docs shows the lines.
