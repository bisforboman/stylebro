### Fixed

- `bro init` and `format` also handle a bulk severity below warning in a folder's own `.editorconfig`
  (`tests/.editorconfig` with `dotnet_analyzer_diagnostic.severity = none`): `init` sets StyleBro's rules by id there, and
  `format` no longer says clean while the build reports them.
- Code from a git submodule or a source generator is left alone also when a project compiles it through a relative
  path (`<Compile Include="..\..\ext\lib\*.cs" />`).
- **BRO1301-BRO1311** no longer rename where IDE0042 (deconstruct variable declaration) would then report, when IDE0042
  is a warning or an error through a category (`dotnet_analyzer_diagnostic.category-Style.severity`) or for every rule
  (`dotnet_analyzer_diagnostic.severity`). IDE1006's severity is read the same way when StyleBro follows your naming rules.
