### New

- **`stylebro-migrate --version`** prints the tool's version.

### Fixed

- **`stylebro-migrate format` no longer reports "clean" when StyleBro's analyzers didn't load.** When no project has
  StyleBro.Analyzers in its restore output it stops with a message before `dotnet format` runs, and it names projects
  without it next to ones with it. Before, a repository with one old-style project skipped the restore entirely, the
  just-added package was never restored, and `format` said clean while the build had thousands of StyleBro errors.
- **Old-style (non-SDK) projects:** `format` now restores the SDK-style projects and leaves only the old-style ones to
  Visual Studio's MSBuild, and gives the `packages.config` restore advice only when a project has `packages.config`.
  `--diff` refuses only `packages.config` projects; other old-style projects are previewed like `format` runs them.
- **`format`'s restore skips NuGet's audit**, so a vulnerable package with `TreatWarningsAsErrors` (NU1901-NU1904) no
  longer stops formatting, also in `--diff` previews that add the StyleBro.Analyzers reference.
- **Solution filters (`.slnf`)** get the same per-framework runs and shared-file handling as their solution; before,
  their projects weren't read and `format` ran without that protection. A solution or filter without C# projects now
  stops with a message.
- **A project MSBuild can't evaluate** (an invalid NuGet.Config, a missing SDK) shows MSBuild's error instead of a
  stack trace.
- **`stylebro-migrate` names the file that references StyleCop** when a `Directory.Build.props` imports it as
  `Directory$(MSBuildProjectExtension).props`.
