### Fixed

- **Line endings when git converts most files:** `init` and `stylebro-migrate --write` no longer set `end_of_line` from
  a few files committed with a fixed ending when git converts most files on checkout (3 CRLF files next to 145
  converted ones wrote `end_of_line = crlf`, and an LF checkout then had thousands of line-ending errors).
- **StyleCop 1.0.x:** the migration reads StyleCop's version from `packages.config` and from `<Analyzer>` hint paths
  into a `packages` folder too, and rules 1.0.x doesn't have (SA1135, SA1136, SA1137, SA1139, SA1314, SA1413) stay off.
- **`--diff` adds the StyleBro reference once** when nested `Directory.Build.props` files import the root one through
  a property (`$(_ParentDirectoryBuildPropsPath)`), instead of in every file (NU1504). Projects that turn central
  package management off get a reference with a version (NU1015 before).
- **Files a project links from another folder** (`<Compile Include="..\Shared\*.cs" />`) are formatted in
  multi-targeted repositories, and `format --files` finds the project that compiles them.
- **Old-style (non-SDK) projects:** `format` skips its `dotnet restore` there (it broke the next Visual Studio build);
  restore with `msbuild -t:restore -p:RestorePackagesConfig=true` first. `--diff` says up front that it can't run on
  them.
- `format --files a.cs --once` and `format --diff --project X.sln` no longer pass `--once` / `--project` on to
  `dotnet format`.
- `format` counts a file as changed only when its content changed; the preview goes on when a project doesn't load (and
  names it), as `format` does; projects skipped because they reference an F# project are reported once, with that
  cause; a run that ends "Not clean" lists the findings kept on purpose too.
