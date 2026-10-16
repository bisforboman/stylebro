# Styles

StyleBro's preset follows StyleCop's defaults. A style is a second layer on top of it, for code that follows another
well-known way of writing C#. There is one: **`microsoft`**, the coding style of
[dotnet/runtime](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md), which much of .NET
copies: `_camelCase` and `s_camelCase` fields, no `this.`, braces optional on one-line bodies, fields at the top of the type.

```xml
<!-- Directory.Build.props -->
<StyleBroPreset>1.0</StyleBroPreset>
<StyleBroStyle>microsoft</StyleBroStyle>
```

The easiest way to set it up is `init`:

```shell
stylebro-migrate init --style microsoft --write
```

It writes both properties and the severities of the built-in .NET rules the style needs into `.editorconfig` (`dotnet
format` reads rule severities only from there). As always, what your code clearly does wins: with `this.` on most member
accesses, `init` turns IDE0003 off instead of removing them; with `count` instead of `_count` fields, it writes
`stylebro_private_field_naming = camelCase`. The report says which conventions it kept.

Plain `init` suggests the style when your code looks like runtime's (`_` and `s_` fields, no `this.`); it never sets it
by itself.

## How it combines

- The style needs the preset: with `<StyleBroPreset>none</StyleBroPreset>` it is ignored, with warning STYLEBRO004. Any
  value other than `microsoft` or `none` fails the build (STYLEBRO003).
- It follows the preset version: it never turns on a rule your `<StyleBroPreset>` doesn't have.
- Your own `.editorconfig` and `.globalconfig` win over it, as they win over the preset.
- `stylebro-migrate --write` (coming from StyleCop) doesn't use a style: its block follows your StyleCop settings, and it
  removes `<StyleBroStyle>` if it finds one. `--style` works with `init` only.

## What it sets

From runtime's guide, its root `.editorconfig` and, for StyleBro's rules neither mentions, the StyleCop severities in
runtime's own build (`eng/CodeAnalysis.src.globalconfig`), read at commit `24543ddd` (2026-10-16).

| Runtime's rule | StyleBro |
|---|---|
| Private and internal fields `_camelCase`, static ones `s_camelCase` (also `static readonly`); constants PascalCase | `stylebro_private_field_naming = _camelCase`, `stylebro_internal_field_naming = like_private`, `stylebro_private_static_field_naming = s_camelCase`, `stylebro_private_mutable_static_field_naming = s_camelCase` ([BRO1303](rules/BRO1303.md), [BRO1306](rules/BRO1306.md)) |
| Fields at the top of the type, nothing else ordered | `stylebro_member_order_scope = fields_first` ([BRO1001](rules/BRO1001.md)) |
| Braces may be left out of a one-line body; required when it spans lines or another block of the chain has them | `csharp_prefer_braces = when_multiline` ([BRO1515](rules/BRO1515.md), [BRO1516](rules/BRO1516.md)) |
| No `this.` unless needed | IDE0003 at warning |
| `var` only where the type is named on the right | `csharp_style_var_*` options, IDE0008 a suggestion |
| `nameof(...)` instead of a string | CA1507 at warning |
| Internal and private types `sealed` or `static` | CA1052 at warning (`private, internal` only), CA1852 a suggestion |
| Pattern matching, `??`, `is null`, `static` local functions, `?.Invoke` | IDE0020, IDE0029, IDE0030, IDE0041, IDE0062, IDE1005 at warning |
| Visibility first, usings outside the namespace and sorted with `System` first, Allman braces, keywords instead of type names, one blank line at most | the preset already does it; `init` writes `dotnet_sort_system_directives_first = true` unless your code clearly doesn't sort its usings |

The rest of runtime's `.editorconfig` code-style options (object and collection initializers, expression bodies, `default`
literals, no index and range operators, ...) are set too; their rules are suggestions, shown in the IDE and not applied by
`dotnet format`, as in runtime's build.

**Off** in the style, because runtime's build has the StyleCop rules they replace off: comment spacing, empty statements,
combined attributes, constants on the left, constructor initializer and parameter list layout, regions inside members,
one field per declaration, `Nullable<T>`, empty comments, literal suffixes, anonymous methods, qualified usings, `base.`,
comments before `{`, variable and type parameter names, `m_` prefixes and underscores in field names, tuple element
casing, trailing commas, unnecessary and precedence parentheses, most blank-line rules (BRO1501-BRO1506, BRO1511,
BRO1513, BRO1518, BRO1519), single-line blocks and accessor layout (BRO1508-BRO1510), and the documentation rules that
reword or add text (BRO1601-BRO1607, BRO1610, BRO1611). StyleBro's rules beyond StyleCop (BRO1133-BRO1151, BRO1408,
BRO1410, BRO1520-BRO1527, BRO1616-BRO1619) are off too: runtime says nothing about them. Turn any of them back on in
`.editorconfig` (`dotnet_diagnostic.BRO1401.severity = warning`).

## Known differences from runtime

- **Constant locals** are PascalCase in runtime's guide; nothing renames them (BRO1301 leaves local constants alone).
- **`if (x) return;` on one line**: the guide wants the statement on its own line; nothing moves it.
- **Non-ASCII characters** as `\uXXXX`: not checked.
- **Target-typed `new()`** only where the type is named on the left: nothing writes the type back into `x = new(...)`.
- **`t_` thread-static fields**: not added (`[ThreadStatic]` fields keep their names).
- **A file's own style wins** (guide rule 9): StyleBro decides per repository (and per folder through `.editorconfig`),
  not per file.
- **CA1852** (seal internal types) is a suggestion, not runtime's warning: the SDK's fix seals a type with `virtual`
  members without removing `virtual`, which doesn't compile.
- **IDE0003** (remove `this.`) is a warning, where runtime's build has it as a suggestion: the guide asks for no `this.`.
- **The file header** (the .NET Foundation's license) isn't set: it's runtime's, not a style. BRO1615 and IDE0073 stay
  your choice.

## Versions

The style is pinned per StyleBro major version: within 1.x its values for existing rules and settings don't change, also
when runtime's guide does (such changes are listed here as differences until 2.0). A minor release decides only its own
new rules, behind the preset version, like the preset (see [versioning](versioning.md#styles)).
