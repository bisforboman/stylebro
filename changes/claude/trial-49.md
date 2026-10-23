### Fixed

- `bro format --engine host` (experimental) crashed loading the projects when the repository's `global.json` selects a
  .NET 8 SDK ("Method 'GetGenericInstantiation' ... does not have an implementation"). The host doesn't support that SDK's
  `dotnet format` (it finds MSBuild through MSBuildLocator): the command now says so and uses `dotnet format`.
- `bro check --build` no longer says "Not verified" when the untouched code's build fails only on CS8002 (a strong-named
  assembly referencing an unsigned one, with warnings as errors): the compiler reports it after StyleBro's rules ran.
- The naming rules (BRO1301-BRO1314) leave a name alone where ReSharper's InconsistentNaming inspection is suppressed
  (`// ReSharper disable once InconsistentNaming` above it, or `disable` up to `restore`), and the renaming fix keeps a
  parameter that has an attribute of its own (`[Argument]`, `[Option]`, `[FromQuery]`, ...), which frameworks bind by
  name: Cocona's command line argument names changed and its tests failed.
