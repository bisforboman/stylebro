# StyleBro

Roslyn analyzers and code fixes that keep C# code tidy. It's a modern alternative to StyleCop, built so that
**`dotnet format` fixes everything it reports**.

> StyleCop writes you a ticket. StyleBro just fixes it. Rule IDs use the `BRO` prefix.

- **Every rule has a code fix, and Fix All works.** Fixes are written for `dotnet format`: deterministic, idempotent,
  and they converge in a single pass. One exception, on purpose: a rename that would break code only the fix can see
  (a name read by reflection, used in generated code) keeps its warning, and `stylebro-migrate format` lists it with
  the reason ([Findings kept on purpose](getting-started.md#findings-kept-on-purpose)).
- **Don't duplicate the SDK.** If a built-in `IDE` rule already covers a StyleCop rule, StyleBro uses that rule instead
  of shipping a copy: the preset sets its options and `stylebro-migrate init` turns it on in `.editorconfig`.
- **Configurable through `.editorconfig`**, with a recommended preset shipped in the package.

## Install

```xml
<PackageReference Include="StyleBro.Analyzers" Version="0.4.0-alpha.1" PrivateAssets="all" />
```

```
dotnet tool install --global StyleBro.Migrate --prerelease
stylebro-migrate init --write      # turns on the built-in rules StyleBro relies on, in .editorconfig
stylebro-migrate format            # whitespace + StyleBro's rules + those built-in rules
```

Coming from StyleCop? Run `stylebro-migrate --write` instead of `init`: see
[Migrating from StyleCop](migrating.md).

## Where next

- [Getting started](getting-started.md): from install to the first `dotnet format` run.
- [Rules](rules/README.md): every rule, by area, with what it replaces and whether it's on by default.
- [Configuration](configuration.md): every `stylebro_*` setting.
- [Baseline](baseline.md): fail only on new violations. [CI](ci.md): checking it in a pipeline.
- [StyleBro vs StyleCop](stylebro-vs-stylecop.md): coverage, speed, correctness and migration, with measured numbers.
- Source, issues and releases: [github.com/bisforboman/stylebro](https://github.com/bisforboman/stylebro).
