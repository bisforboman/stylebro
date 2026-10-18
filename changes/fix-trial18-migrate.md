### Fixed

- **`bro init`** writes StyleBro's rules by id where the `.editorconfig` silences analyzers in bulk
  (`dotnet_analyzer_diagnostic.severity = silent`). Before, `format` skipped every StyleBro rule and said clean, while
  the build reported them. **`bro format`** no longer says clean while such a setting hides rules from it.
- **`bro init`** and **`--write`** recognize CSharpier in every folder that configures it, not only the
  first one, so StyleBro's layout rules no longer fight CSharpier in the other folders.
- StyleBro no longer reports code that a project compiles from a git submodule. `format` can't fix it there.
- **`bro`** names a StyleCop reference that comes from a git submodule and says where the package swap
  goes instead (the repository's own `Directory.Build.props`). After that swap, a second `--write` recognizes the
  migrated setup instead of calling it leftovers.
- **`bro init`** no longer makes BRO1404 add `public` to interface members when the `.editorconfig` says
  `dotnet_style_require_accessibility_modifiers = always`, nothing enforces that in the build, and the code's interface
  members don't have access modifiers.
- **`bro init`** writes the same block when it runs a second time on a repository that uses CSharpier, and
  doesn't warn about IDE0055 crashes where CSharpier turns IDE0055 off.
- **`bro init`** explains how to keep a setting it overrides (such as `end_of_line = crlf` in files that
  are mostly LF): set it again below the block.
- **`--diff`** really leaves `packages.config` projects out of the patch. **`packages.config`** files written on one
  line get StyleBro's entry on a line of its own, and NuGet's `EnsureNuGetPackageBuildImports` target is found when its
  name is in single quotes, so a second target no longer replaces it.
