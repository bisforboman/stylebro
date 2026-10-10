# StyleBro

Roslyn analyzers and code fixes that keep C# code tidy. It's a modern alternative to StyleCop, built so that **`dotnet format` fixes everything it reports**.

> StyleCop writes you a ticket. StyleBro just fixes it. Rule IDs use the `BRO` prefix.

**Documentation:** [bisforboman.github.io/stylebro](https://bisforboman.github.io/stylebro/) (the `docs/` folder as a
searchable site).

**New here?** [Getting started](https://github.com/bisforboman/stylebro/blob/main/docs/getting-started.md): from install to the first `dotnet format` run, for a new
project or coming from StyleCop.

**Deciding whether to switch from StyleCop?** [StyleBro vs StyleCop](https://github.com/bisforboman/stylebro/blob/main/docs/stylebro-vs-stylecop.md): coverage, speed,
correctness and migration, with measured numbers.

## Principles

- **Every rule has a code fix, and Fix All works.** Fixes are written for `dotnet format`: deterministic, idempotent, and they converge in a single pass. CI checks this with a second `dotnet format --verify-no-changes` run.
- **Don't duplicate the SDK.** If a built-in `IDE` rule already covers a StyleCop rule, StyleBro uses that rule instead of shipping a copy: the preset sets its options and `stylebro-migrate init` turns it on in `.editorconfig`.
- **Configurable through `.editorconfig`**, with a recommended preset shipped as a low-priority global config.

## Rules

128 rules, each with a code fix. The full list, with what each one replaces and whether it's on by
default: **[docs/rules](https://github.com/bisforboman/stylebro/blob/main/docs/rules/README.md)**.

| Area | Rules | For example |
|------|------:|-------------|
| [Ordering, spacing and comments](https://github.com/bisforboman/stylebro/blob/main/docs/rules/README.md#ordering-spacing-and-comments) | 7 | member order, accessor order, comment and directive spacing |
| [Readability](https://github.com/bisforboman/stylebro/blob/main/docs/rules/README.md#readability) | 51 | `string.Empty`, `else if`, list and query layout, lambdas, tuples, null checks |
| [Naming](https://github.com/bisforboman/stylebro/blob/main/docs/rules/README.md#naming) | 14 | casing of fields, parameters and types, `I`/`T` prefixes, `Async` suffix |
| [Maintainability](https://github.com/bisforboman/stylebro/blob/main/docs/rules/README.md#maintainability) | 10 | access modifiers, parentheses, trailing commas |
| [Layout](https://github.com/bisforboman/stylebro/blob/main/docs/rules/README.md#layout) | 27 | blank lines, braces, single-line blocks, line breaks when wrapping |
| [Documentation](https://github.com/bisforboman/stylebro/blob/main/docs/rules/README.md#documentation) | 19 | `<inheritdoc/>`, periods, summary wording, file headers |

How every StyleCop rule maps to StyleBro or the SDK: [docs/stylecop-mapping.md](https://github.com/bisforboman/stylebro/blob/main/docs/stylecop-mapping.md). The rules
StyleBro doesn't cover, why, and what it would take to add them: [docs/skipped-rules.md](https://github.com/bisforboman/stylebro/blob/main/docs/skipped-rules.md).
Where the preset and the rules differ from StyleCop's defaults, and how to get StyleCop's behavior back:
[docs/differences-from-stylecop.md](https://github.com/bisforboman/stylebro/blob/main/docs/differences-from-stylecop.md).

## Usage

```xml
<PackageReference Include="StyleBro.Analyzers" Version="0.4.0-alpha.1" PrivateAssets="all" />
```

Then, once per repository, turn on the built-in .NET rules StyleBro relies on (IDE0055 formatting, IDE0036
modifier order, ...). Their severities have to be in `.editorconfig`: `dotnet format` ignores severities
from a package's preset.

```
dotnet tool install --global StyleBro.Migrate --prerelease
stylebro-migrate init --write      # adds a block to .editorconfig
```

```
stylebro-migrate format            # whitespace + StyleBro's rules + the built-in rules init turns on
dotnet format analyzers --diagnostics BRO1001
```

The preset (StyleBro's rule severities and the formatting options) comes with the package. To opt out and configure
everything yourself: `<StyleBroPreset>none</StyleBroPreset>`. Coming from StyleCop? Use `stylebro-migrate --write`
instead of `init` (below). Checking it in CI: [docs/ci.md](https://github.com/bisforboman/stylebro/blob/main/docs/ci.md).

`stylebro-migrate format` is `dotnet format` (same options) limited to StyleBro's and those built-in rules: plain
`dotnet format` also applies every other analyzer's and the compiler's fixes, which can break the build. With several
target frameworks it runs once per framework, where plain `dotnet format` crashes on the SDK's formatting fix (details
in [docs/ci.md](https://github.com/bisforboman/stylebro/blob/main/docs/ci.md)).

## Migrating from StyleCop

`stylebro-migrate` reads a repository's StyleCop setup (rulesets, global configs, `.editorconfig` files,
`stylecop.json`) and writes matching StyleBro and SDK settings into its `.editorconfig` files, so the switch doesn't
reformat code StyleCop was happy with. It also carries `#pragma warning disable SA…` and `[SuppressMessage]`
suppressions over to the replacing rules, and lists the StyleCop rules nothing enforces any more.
`dotnet tool install --global StyleBro.Migrate --prerelease`, then `stylebro-migrate path/to/repo`. See
[docs/migrating.md](https://github.com/bisforboman/stylebro/blob/main/docs/migrating.md).

## Baseline: fail only on new violations

In a codebase with many existing violations, `stylebro-migrate baseline` records them in `stylebro.baseline`. The
build, the IDE and `dotnet format` then ignore those (every StyleBro rule and the SDK rules `stylebro-migrate init`
turns on), so only new code has to follow the rules; a violation counts as new once its line is edited. See
[docs/baseline.md](https://github.com/bisforboman/stylebro/blob/main/docs/baseline.md).

## Contributing

Repository layout, build and test commands, and the design rules every rule follows: [CONTRIBUTING.md](https://github.com/bisforboman/stylebro/blob/main/CONTRIBUTING.md).
