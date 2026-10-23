### Fixed

- `bro format --engine host` (experimental) crashed loading the projects when the repository's `global.json` selects a
  .NET 8 SDK and a newer .NET runtime is installed ("Method 'GetGenericInstantiation' ... does not have an
  implementation"). It now takes the runtime's copy of an assembly the SDK's `dotnet format` also ships, as `dotnet format`
  itself does.
- `bro check --build` no longer says "Not verified" when the untouched code's build fails only on CS8002 (a strong-named
  assembly referencing an unsigned one, with warnings as errors): the compiler reports it after StyleBro's rules ran.
