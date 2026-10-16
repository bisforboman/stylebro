### Changed

- **`stylebro-migrate init`** turns BRO1519 off when the code is mixed about blank lines after a block's `}`, like every
  other mixed convention, instead of adding hundreds of blank lines.
- **`stylebro-migrate init`** writes `s_camelCase` (`stylebro_private_static_field_naming`,
  `stylebro_private_mutable_static_field_naming`) when private static fields mostly have the `s_` prefix, instead of
  turning BRO1307 off.
- **`stylebro-migrate init`** no longer allows one-line jumps (`if (x) return;`) when your .editorconfig enforces braces
  (IDE0011 at warning or error).
- **`stylebro-migrate --write`** with StyleSharp: SST1149 (`is null` checks) turns on BRO1133 with
  `stylebro_null_check_style = pattern_matching`, so `null == x` no longer becomes `x == null`.

### Fixed

- **`stylebro-migrate init`** writes `<StyleBroPreset>` on a line of its own into an empty `<PropertyGroup></PropertyGroup>`
  (CSharpier's check rejected the closing tag on the same line), and keeps a self-closing `<Project Sdk="..." />` intact.
- **`stylebro-migrate init`** counts only compiled C# files: Verify snapshots (`*.verified.cs`, `*.received.cs`) and files a
  project with its own Compile items leaves out no longer decide the conventions.
- **`stylebro-migrate init`** no longer crashes on a file it can't read (a log another process holds open in the
  repository).
- **`stylebro-migrate --write`**: with SA1633 off, the report no longer lists SA1634-SA1641 as not enforced (StyleCop checks
  no header then), and it mentions that StyleBro.Analyzers can go once into the root Directory.Build.props when every
  project references StyleCop.
- **`stylebro-migrate init`**'s report names the doc elements found out of order, and mentions the template's empty lines
  only when there is a file header template.
