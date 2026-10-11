# StyleBro in packages.config projects (2026-10-11)

## Question

The eighth trial (xbox-live-developer-tools: old-style .NET Framework projects with packages.config, StyleCop 1.0.2 as
`<Analyzer>` items) added StyleBro as a `PackageReference` in Directory.Build.props. That switched the projects to
PackageReference restore: their packages.config packages weren't restored any more, and a fresh clone didn't build. How
should StyleBro go into such projects?

## Choices

- Install it the packages.config way: a packages.config entry, the `<Analyzer>` items and the build targets import, as
  NuGet writes them (and as StyleCop was installed there); keep Directory.Build.props' PackageReference away from them.
- Tell users to migrate those projects to PackageReference first.
- Keep the PackageReference and document that the `packages` folder must exist.

## Decision

"Install the packages.config way." The preview's package step installs it into each packages.config project like NuGet
does, and a PackageReference in a Directory.Build.props those projects import gets
`Condition="!Exists('$(MSBuildProjectDirectory)\packages.config')"`. The migration's report and the docs say the same.
Checked on a fresh copy of xbox (no `packages` folder): MSBuild restore, build with StyleBro's warnings, `format`, build.
