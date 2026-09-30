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
| [BRO1101](docs/rules/BRO1101.md) | Code should not contain empty statements | SA1106 | Yes |
| [BRO1102](docs/rules/BRO1102.md) | Each attribute should be in its own brackets | SA1133 | Yes |

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
