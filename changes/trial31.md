### Fixed

- **init** no longer marks a whole repository as vendored code when its root has several license files with different
  holders (a GPL `COPYING` next to the team's own `LICENSE.md`): every root license counts as the team's own, and the root
  folder is never vendored. Before, init wrote `generated_code = true` for everything and counted no conventions.
- **init** and **migrate** skip extracted NuGet packages inside the repository (a `nuget.config` packages folder or
  `packages/` from packages.config): a package's content files were counted as code and could be marked vendored.
- **`--diff`** leaves out a NuGet packages folder that a `nuget.config` puts inside the repository: the preview counted every
  restored package file as a change.
- Renames (BRO1303, BRO1306-BRO1308) keep a static field that registers a XAML property or routed event (`LevelProperty`,
  `IsOpenPropertyKey`, `ClickEvent` in Avalonia, WPF, WinUI or MAUI): XAML and Avalonia's analyzers find it by that name. It
  is listed as kept, with the reason.
- **BRO1123** writes `(0D, 1L)` instead of `((double)0, (long)1)` for `new ValueTuple<double, long>(0, 1)` when BRO1122 is
  on, so `dotnet format` doesn't need a second run for BRO1122.
