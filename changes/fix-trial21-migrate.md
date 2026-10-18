### Changed

- **`init`** follows two more conventions of your code: types without `internal` (then BRO1404 stays off, as no
  setting asks for modifiers on types only) and `case X:` blocks at the label's level
  (`csharp_indent_case_contents_when_block = false`).
- **`format`** removes the usings its fixes leave unused in the same run where IDE0005 is enforced, one run sooner.
- **`bro migrate`** says your `stylecop.json` can go once StyleCop.Analyzers is gone; the preview counts carried-over
  suppressions the same way `--write` does.

### Fixed

- **`init`** recognizes CSharpier set up above a folder whose `.editorconfig` says `root = true` (CSharpier in
  `.config/dotnet-tools.json`, code under `src/`): that folder gets the CSharpier settings, and `format` says to run
  CSharpier afterwards.
- **`init`** and the preview write `<StyleBroPreset>` and the package reference in the file's own indentation, and the
  package version next to the other analyzers in `Directory.Packages.props`.
- **`format --verify-no-changes`** no longer says "Clean" when findings are left that no fix changes (like IDE0049 in a
  using alias): it lists them and fails.
- **`format`** says how to format a project no solution configuration builds; its note on generated files leaves out
  resource and WinForms designer files and mentions other projects' generated files; and it no longer says fixes still
  run in a folder that turns every analyzer off.
- **`init`**'s report: clearer labels for primary-constructor parameter names, and no "nothing enforces it in the build"
  when `EnforceCodeStyleInBuild` is on.
