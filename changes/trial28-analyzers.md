### Changed

- Names used as structured log keys keep them: a variable, parameter, field, property or tuple element used in an
  interpolated string's hole whose handler takes the hole's text as a `[CallerArgumentExpression]` argument (ZLogger's
  `$"{UserId}"` logs the key `UserId`) isn't renamed by BRO1301-BRO1311. `bro format` lists it as kept, with
  the handler's name, and baselines it.
- Files a T4 template writes (a Compile item with `DependentUpon` `*.tt` or `AutoGen` True) count as generated code:
  StyleBro and the built-in rules it turns on report nothing there, and `bro format` leaves the C# file next
  to each `.tt` file alone.

### Fixed

- **BRO1309** and the field rules (BRO1303, BRO1306-BRO1308) no longer rename a `new` member that hides a base member by
  its name (`private new Settings configData => ...`): renamed, it hid nothing and the build failed on CS0109 with
  warnings as errors. A base member that a derived type hides that way keeps its name too.
- **BRO1131** and **BRO1301** give the same code in either order: when a local named like a method (`var Download = ...`)
  is renamed, the `this.` BRO1131 wrote for `base.Download(...)` goes too where `dotnet_style_qualification_for_method`
  is false.
- **BRO1305** renames a type parameter in the crefs of its own type or method too (`<see cref="Node{_TVertex}"/>`), so
  the standard constructor summary is right in the same run.
- XAML files keep fewer names by mistake: words in text for people (`Text="... window width"`, `ToolTip`, `Content`,
  `Header`, `Title`) and the `x` of `{x:Static ...}` or `{x:Null}` no longer keep a member's name. A type or property
  named by a XAML element or attribute is listed as such ("renaming would break the XAML build").
- In a git submodule's own checkout, StyleBro reports the submodule's findings again: the superproject's `.gitmodules`
  above it named the submodule's own folder, which hid all of them.
