# StyleBro

Roslyn analyzers and code fixes that keep C# code tidy. It's a modern alternative to StyleCop, built so that **`dotnet format` fixes everything it reports**.

> StyleCop writes you a ticket. StyleBro just fixes it. Rule IDs use the `BRO` prefix.

## Principles

- **Every rule has a code fix, and Fix All works.** Fixes are written for `dotnet format`: deterministic, idempotent, and they converge in a single pass. CI checks this with a second `dotnet format --verify-no-changes` run.
- **Don't duplicate the SDK.** If a built-in `IDE` rule already covers a StyleCop rule, the preset enables that rule instead of shipping a copy.
- **Configurable through `.editorconfig`**, with a recommended preset shipped as a low-priority global config.

## Rules

| ID | Title | Replaces | Fix |
|----|-------|----------|-----|
| [BRO1001](docs/rules/BRO1001.md) | Members should be ordered | SA1201-SA1204, SA1214 | Yes, with custom Fix All |
| [BRO1002](docs/rules/BRO1002.md) | Single-line comments should begin with a space | SA1005 | Yes |
| [BRO1101](docs/rules/BRO1101.md) | Code should not contain empty statements | SA1106 | Yes |
| [BRO1102](docs/rules/BRO1102.md) | Each attribute should be in its own brackets | SA1133 | Yes |
| [BRO1103](docs/rules/BRO1103.md) | Constants should be on the right-hand side of comparisons | SA1131 | Yes |
| [BRO1104](docs/rules/BRO1104.md) | Use default instead of a value type's default constructor | SA1129 | Yes |
| [BRO1105](docs/rules/BRO1105.md) | Constructor initializers should be on their own line | SA1128 | Yes |
| [BRO1106](docs/rules/BRO1106.md) | Use string.Empty for empty strings | SA1122 | Yes |
| [BRO1107](docs/rules/BRO1107.md) | Split parameters should start on the line after the declaration | SA1116 | Yes |
| [BRO1108](docs/rules/BRO1108.md) | Parameters should be on the same line or on separate lines | SA1117 | Yes |
| [BRO1301](docs/rules/BRO1301.md) | Variable names should begin with a lower-case letter | SA1312 | Yes |
| [BRO1302](docs/rules/BRO1302.md) | Parameter names should begin with a lower-case letter | SA1313 | Yes |
| [BRO1303](docs/rules/BRO1303.md) | Private field names should be camelCase | SA1306, SA1309 | Yes |
| [BRO1304](docs/rules/BRO1304.md) | Interface names should begin with I | SA1302 | Yes |
| [BRO1305](docs/rules/BRO1305.md) | Type parameter names should begin with T | SA1314 | Yes |
| [BRO1401](docs/rules/BRO1401.md) | Use a trailing comma in multi-line initializers | SA1413 | Yes |
| [BRO1501](docs/rules/BRO1501.md) | Opening braces should not be preceded by a blank line | SA1509 | Yes |
| [BRO1502](docs/rules/BRO1502.md) | Chained blocks should not be preceded by a blank line | SA1510 | Yes |
| [BRO1503](docs/rules/BRO1503.md) | Opening braces should not be followed by a blank line | SA1505 | Yes |
| [BRO1504](docs/rules/BRO1504.md) | Single-line comments should be preceded by a blank line | SA1515 | Yes |
| [BRO1505](docs/rules/BRO1505.md) | Elements should be separated by a blank line | SA1516 | Yes |
| [BRO1506](docs/rules/BRO1506.md) | Single-line comments should not be followed by a blank line | SA1512 | Yes |
| [BRO1507](docs/rules/BRO1507.md) | Files should not end with blank lines | SA1518 | Yes |

How every StyleCop rule maps to StyleBro or the SDK: [docs/stylecop-mapping.md](docs/stylecop-mapping.md).

## Usage

```xml
<PackageReference Include="StyleBro.Analyzers" Version="0.1.0-alpha.1" PrivateAssets="all" />
```

```
dotnet format                      # whitespace + style + analyzers, including StyleBro
dotnet format analyzers --diagnostics BRO1001
```

To opt out of the preset and configure everything yourself: `<StyleBroPreset>none</StyleBroPreset>`.

## Repository layout

```
src/StyleBro.Analyzers     netstandard2.0, Microsoft.CodeAnalysis.CSharp only (no Workspaces, RS1038)
src/StyleBro.CodeFixes     netstandard2.0, code fixes + Fix All providers
src/StyleBro.Package       packs both DLLs into analyzers/dotnet/cs + build/ preset
tests/StyleBro.Tests       Microsoft.CodeAnalysis.Testing: diagnostics, single fix, Fix All, idempotence
samples/Messy           dotnet format integration sample (Input -> Expected)
scripts/verify-format.ps1
```

## Developing

```
dotnet test tests/StyleBro.Tests
pwsh scripts/verify-format.ps1
dotnet pack src/StyleBro.Package -c Release -o artifacts
```

Roslyn is pinned to 4.8.0 so the package loads in the .NET 8 SDK / VS 17.8 and newer.

## Roadmap

1. `BRO1600`-series documentation: generate XML doc stubs, `<inheritdoc/>` on overrides and interface implementations, keep `<param>` in sync with parameters
2. StyleCop migration tool: read `stylecop.json` + rulesets and write equivalent `.editorconfig`
3. Layout rules the SDK only has as experimental (blank lines, IDE2000 series)
4. Baseline support: fail only on new violations in legacy codebases
