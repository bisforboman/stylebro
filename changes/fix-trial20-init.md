### Changed

- `bro init` honors folders that turn every analyzer off (`dotnet_analyzer_diagnostic.severity = none` or
  `silent` in their own `.editorconfig`, such as test samples): it turns StyleBro's rules, IDE0055 and the other built-in
  rules it manages off there by id, so the build and `format` leave those files alone. Before, it turned StyleBro's rules
  on there, and `format` rewrote the samples. Rules the folder's `.editorconfig` sets by id stay as they are.
- `init` keeps your own IDE0055 severity (e.g. `suggestion`) instead of writing `warning`.
- `init` detects three more conventions: a blank line between a doc comment and its element (BRO1511), primary
  constructor parameters named like fields (`_logger`: BRO1302 is turned off, it has no switch for those alone), and
  the casing of tuple element names (`stylebro_tuple_element_name_casing`).
- The block `init` writes into a folder's own `.editorconfig` is now called `stylebro-folder` (it holds rule severities
  too, not only whitespace settings); an existing `stylebro-whitespace` block is replaced.

### Fixed

- `init` counts IDE0040 and IDE0009 as enforced through a category or bulk severity
  (`dotnet_analyzer_diagnostic.category-Style.severity = warning`), and no longer counts
  `dotnet_style_require_accessibility_modifiers = always` without a build severity as enforced: **BRO1404** then no
  longer adds `public` to interface members the build never asked for.
- `init` no longer reads `} else return x;` inside a one-line method as "else on the `}` line", which turned the code
  into K&R braces.
- `init` no longer sets the preset version in `Directory.Build.props` files of example folders whose projects aren't in
  a solution and don't use StyleBro.
- Report texts: a Sonar rule in a `<NoWarn>` says the option it no longer sets, and the line-ending verdict for files git
  converts no longer starts with a count of the other files.
