### Changed

- **CSharpier:** in a repository that uses CSharpier (a `.csharpierrc*`, the `csharpier` tool in
  `dotnet-tools.json`, or `CSharpier.MsBuild`), `init` turns off IDE0055 and the 32 StyleBro rules for layout CSharpier
  decides (line breaks, wrapping, single-line blocks, the blank lines it normalizes), so the two formatters no longer
  undo each other. Naming, member order, documentation, braces and readability rules stay. The list is in Getting
  started.
- **Member order follows the code:** `init` counts the types already in BRO1001's order; mostly unsorted or mixed code
  gets `dotnet_diagnostic.BRO1001.severity = none` instead of every type being reordered.
- **Unsigned literal suffixes follow the code:** `init` counts `0u` against `0U`; mostly lower case or mixed turns
  BRO1135 off.
- **Silent keys don't count:** a key set at severity `silent` or `none` (`csharp_prefer_braces = true:silent`, or its
  IDE rule at `silent`) isn't enforced, so `init` follows the code instead of the key.

### Fixed

- `init` counts the projects that target several frameworks through MSBuild, so frameworks set through a property
  (`$(DefaultTargetFrameworks)`) count too.

### Upgrading

Run `stylebro-migrate init --write` again to pick these up. Member order and suffixes are counted on the code as it
is now: after a `format` run that already sorted it, BRO1001 stays on.
