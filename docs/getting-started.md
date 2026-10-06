# Getting started

From install to the first `dotnet format` run. Pick the path that fits:

- [A. A new project, or no StyleCop](#a-a-new-project-or-no-stylecop)
- [B. Coming from StyleCop](#b-coming-from-stylecop)

Both use two packages: **StyleBro.Analyzers** (the rules, referenced by your projects) and **StyleBro.Migrate** (the
`stylebro-migrate` command line tool, used once to write settings):

### Which version

The latest release on nuget.org is **0.1.0-alpha.8**. This page describes the code on `main`, which is ahead of it:

- `stylebro-migrate init`, `stylebro-migrate format` and the migration fixes on this page aren't in alpha.8;
- alpha.8's package doesn't hand its preset to the compiler (fixed since), so its rules run at their default severities;
- many rules are newer than alpha.8 ([backlog.md](backlog.md) lists them).

Until the next release, build both packages from a clone into a local folder and use that version (any version
string works; `0.1.0-dev.1` here):

```
git clone https://github.com/bisforboman/stylebro
dotnet pack stylebro/src/StyleBro.Package -o stylebro-feed -p:Version=0.1.0-dev.1
dotnet pack stylebro/src/StyleBro.Migrate -o stylebro-feed -p:Version=0.1.0-dev.1
dotnet tool install --global StyleBro.Migrate --add-source stylebro-feed --version 0.1.0-dev.1
```

Add the folder as a package source in your repository's `nuget.config`
(`<add key="stylebro" value="path/to/stylebro-feed" />`) and use `0.1.0-dev.1` wherever this page says
`0.1.0-alpha.8`. With alpha.8 from nuget.org instead (`dotnet tool install --global StyleBro.Migrate --prerelease`), only
`stylebro-migrate path/to/repo [--write]` and `stylebro-migrate baseline` exist.

## A. A new project, or no StyleCop

### 1. Add the package

Add it to every project, for example in `Directory.Build.props` at the repository root:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="StyleBro.Analyzers" Version="0.1.0-alpha.8" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

With central package management, the version goes in `Directory.Packages.props` and the reference has none:

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="StyleBro.Analyzers" Version="0.1.0-alpha.8" />
</ItemGroup>

<!-- Directory.Build.props -->
<ItemGroup>
  <PackageReference Include="StyleBro.Analyzers" PrivateAssets="all" />
</ItemGroup>
```

The package brings StyleBro's rules and its preset: rule severities and formatting options, close to StyleCop's
defaults. Your own `.editorconfig` wins over the preset. To configure everything yourself instead:
`<StyleBroPreset>none</StyleBroPreset>`.

### 2. Turn on the built-in rules

StyleBro relies on some built-in .NET rules (IDE0055 formatting, IDE0036 modifier order, IDE0065 using placement, ...).
Their severities have to be in `.editorconfig`, because `dotnet format` ignores rule severities in a package's preset.
In the repository root:

```
stylebro-migrate init            # shows the block
stylebro-migrate init --write    # adds it to .editorconfig (created if missing)
```

The block sits between `# BEGIN stylebro-migrate` and `# END stylebro-migrate`; running the command again replaces it.
Put your own settings outside it. To get these rules reported by the build too, add
`<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` to `Directory.Build.props`.

### 3. Run dotnet format

```
dotnet format                     # every project targets one framework
stylebro-migrate format           # some project sets several <TargetFrameworks>
```

This fixes whitespace, the built-in rules and every StyleBro rule in one go. A second run should change nothing.
Review the diff and commit it. `init` tells you which of the two commands your repository needs; why plain
`dotnet format` doesn't work with several target frameworks: [Multi-targeted repositories](#multi-targeted-repositories).
In a large codebase you may not want one big change: see [Large codebases](#large-codebases).

[What to expect on the first run](#what-to-expect-on-the-first-run): solution names, run time, warnings as errors.

### 4. Check it in CI

Run `dotnet format --verify-no-changes --severity warn` after the build: it fails when `dotnet format` would change a
file. Set-up for GitHub Actions and Azure Pipelines: [ci.md](ci.md).

## B. Coming from StyleCop

Still deciding? [StyleBro vs StyleCop](stylebro-vs-stylecop.md) compares the two with measured numbers.

`stylebro-migrate` reads your StyleCop setup (rulesets, global configs, `.editorconfig` files, `stylecop.json`) and
writes StyleBro and SDK settings that enforce the same things, so code StyleCop was happy with stays as it is. A
walk-through on a small project, with real output: [samples/StyleCopMigration](../samples/StyleCopMigration/README.md).

### 1. Dry run

```
stylebro-migrate path/to/repo
```

It prints what it read, the settings it would write, and the StyleCop rules nothing enforces after the switch (dropped
by design, partly covered, or not covered yet). Nothing is changed.

### 2. Write the settings

```
stylebro-migrate path/to/repo --write
```

This writes the settings into your `.editorconfig` files (between the `stylebro-migrate` markers), turns StyleBro's
preset off in the root `Directory.Build.props` (the settings replace it), and adds the replacing rule ids to existing
StyleCop suppressions (`#pragma warning disable`, `[SuppressMessage]`, `<NoWarn>`). You don't need `init` after this:
the block includes the built-in rules.

### 3. Swap the package

Remove the StyleCop reference and add `StyleBro.Analyzers` instead, as in [path A, step 1](#1-add-the-package). Look
for both package ids, `StyleCop.Analyzers` and the prerelease `StyleCop.Analyzers.Unstable`, and for a
`GlobalPackageReference` in `Directory.Packages.props` (central package management): replace it with
`<GlobalPackageReference Include="StyleBro.Analyzers" Version="..." />`. `stylecop.json` and the StyleCop suppressions
can stay; StyleBro ignores them.

### 4. Run dotnet format

```
dotnet format                     # or, if any project sets several <TargetFrameworks>:
stylebro-migrate format
```

`--write` ends with the command your repository needs ("Next: ..."). In a StyleCop-clean repository this should change
little or nothing. Look at what it did change: usually code StyleCop
missed, or a rule where StyleBro deliberately differs ([differences-from-stylecop.md](differences-from-stylecop.md)).
Then check the report from step 1 for rules your team relied on that nothing enforces any more. Details:
[migrating.md](migrating.md). CI: [ci.md](ci.md).

## Newer C# and APIs (optional)

To also rewrite code into newer C# and newer APIs (target-typed `new()`, collection expressions, file-scoped
namespaces, `ArgumentNullException.ThrowIfNull`, ...) with the SDK's own rules, after either path:

```
stylebro-migrate init --modernize --write
dotnet format
```

In multi-targeted projects, StyleBro's multi-target guard hides the newer-API rules where a target framework lacks the
API, and the newer-C# rules are suggestions unless the project sets `LangVersion`. Tiers, examples and the rules left out:
[modernizing.md](modernizing.md).

## What to expect on the first run

- **Name the solution when the root has several.** With more than one project or solution file in the folder (say
  `Build.csproj` next to `MySolution.slnx`), `dotnet format` stops with MSB1011 ("more than one project or solution
  file"). Pass the one to format: `dotnet format MySolution.slnx` (the same for `stylebro-migrate format`).
- **Multi-targeted repositories take longer.** `stylebro-migrate format` runs `dotnet format` once per target
  framework, and each run loads the solution again. That's minutes, not seconds: Dapper, which builds in 13 seconds,
  took 345 seconds to format.
- **`TreatWarningsAsErrors` fails the first build** until everything is fixed or recorded: StyleBro's findings are
  warnings, so they become errors. Run `dotnet format` (or `stylebro-migrate format`) before building, or record what's
  left in a [baseline](baseline.md).
- **Some renames are left to you.** The naming rules don't rename a member whose name also appears in a string
  (reflection, serialized names, `DebuggerDisplay`): the rename would compile but break at run time. Those warnings
  stay after `dotnet format`. Rename them by hand, suppress them (`#pragma warning disable BRO1303` with a reason), or
  record them in the [baseline](baseline.md).

## Excluding vendored code

Code you don't own (a copied library, generated sources checked in, a `ThirdParty` folder) shouldn't be reformatted.
Mark it as generated code in the root `.editorconfig`:

```ini
[ThirdParty/**]
generated_code = true
```

StyleBro's rules, the built-in rules (IDE0055, IDE0073, ...) and `dotnet format`'s whitespace pass all skip generated
code, in the build and in `dotnet format`. One side effect: the compiler treats generated files as nullable-oblivious
unless they say `#nullable enable` themselves.

`dotnet_analyzer_diagnostic.severity = none` in that folder isn't enough: a rule turned on by its id
(`dotnet_diagnostic.BRO1001.severity = warning` in the root) wins over it. `stylebro-migrate --write` therefore writes
every replacing rule as `none` into such a folder's `.editorconfig`, and it never edits files inside git submodules
(folders with their own `.git`).

**Files linked from a submodule.** A project can compile files from another folder (`<Compile Include="..\..\ext\Lib\Code.cs" />`).
They're checked like the project's own files, but `stylebro-migrate format` only formats each project's own folder,
so their warnings stay. Mark the submodule's folder as generated code too, with a section for its path in the root
`.editorconfig` (`[ext/**]`, then `generated_code = true`).

## Multi-targeted repositories

If any project sets several `<TargetFrameworks>`, use `stylebro-migrate format` wherever this page says
`dotnet format`. It takes the same arguments and options:

```
stylebro-migrate format
stylebro-migrate format MySolution.sln --verify-no-changes --severity warn
```

Plain `dotnet format` crashes there on the SDK's formatting fix (IDE0055) without writing anything. The command runs
`dotnet format` once per target framework instead; without multi-targeted projects it's one plain run. A diagnostic
every framework sees is printed once, and `--report` writes one `format-report.json` covering all runs. Why:
[ci.md](ci.md#notes). `stylebro-migrate init` tells you when it finds such projects.

## Large codebases

If `dotnet format` would change too much at once, record today's violations in a baseline instead:

```
stylebro-migrate baseline                            # in the folder with your solution
stylebro-migrate baseline --project MySolution.sln   # or name it
```

This writes `stylebro.baseline`; commit it. The build, the IDE and `dotnet format` then ignore those violations, and
only new code has to follow the rules. A baseline can't hide whitespace formatting. Details: [baseline.md](baseline.md).
