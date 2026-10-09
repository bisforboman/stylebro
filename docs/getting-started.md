# Getting started

From install to the first `dotnet format` run. Pick the path that fits:

- [Preview first](#preview-first): what either path would change, without changing anything
- [A. A new project, or no StyleCop](#a-a-new-project-or-no-stylecop)
- [B. Coming from StyleCop](#b-coming-from-stylecop)

Both use two packages: **StyleBro.Analyzers** (the rules, referenced by your projects) and **StyleBro.Migrate** (the
`stylebro-migrate` command line tool, used once to write settings):

### Which version

The latest release on nuget.org is **0.3.0-alpha.1**, a prerelease: install the tool with `--prerelease` and reference
that exact version. Everything on this page is in it. To try the code on `main` before the next release, build both
packages from a clone into a local folder (any version string works; `0.3.0-dev.1` here):

```
git clone https://github.com/bisforboman/stylebro
dotnet pack stylebro/src/StyleBro.Package -o stylebro-feed -p:Version=0.3.0-dev.1
dotnet pack stylebro/src/StyleBro.Migrate -o stylebro-feed -p:Version=0.3.0-dev.1
dotnet tool install --global StyleBro.Migrate --add-source stylebro-feed --version 0.3.0-dev.1
```

Then add the folder as a package source in your repository's `nuget.config`
(`<add key="stylebro" value="path/to/stylebro-feed" />`) and use `0.3.0-dev.1` wherever this page says `0.3.0-alpha.1`.

## Preview first

Before changing anything, `--diff` shows what a path would do to your code. It works on all three commands:

```
stylebro-migrate init --diff          # path A: init's settings, then format
stylebro-migrate path/to/repo --diff  # path B: the migration's settings (--write), then format
stylebro-migrate format --diff        # only the format run
```

The preview copies the repository to a temporary folder (git's tracked and untracked files, not the ignored ones; the
working trees of submodules too), runs the command there, then `stylebro-migrate format` until a run changes nothing,
and deletes the copy. Your repository isn't touched. If it doesn't reference StyleBro.Analyzers yet, the copy gets the
reference from [step 1](#1-add-the-package) (the tool's own version) and the output says so. It prints a summary and
writes the full diff to `stylebro-preview.patch` in the current folder (`--diff=other.patch` names another file).
`--keep` keeps the copy and prints where it is; `--all` passes on to format; with several solutions or projects in the
folder, `--project MySolution.sln` names the one to format. FFMpegCore, `stylebro-migrate init --diff`:

```text
Previewing 'stylebro-migrate init --write' and 'stylebro-migrate format' on a copy; nothing in C:\src\FFMpegCore changes.
Copying the repository to C:\Users\me\AppData\Local\Temp\stylebro-preview-fmsw0dps.3qa\FFMpegCore... 0s
Settings: 31 lines in .editorconfig; 3 lines in Directory.Build.props (StyleBro.Analyzers)
  StyleBro.Analyzers wasn't referenced: added StyleBro.Analyzers 0.3.0-alpha.1 to Directory.Build.props for the preview, as docs/getting-started.md says (the patch includes it).
Finding what format fixes ('dotnet format --verify-no-changes')... 16s
Format run 1... 29s
Format run 2... 14s
Format: 124 files, 579 reported changes, clean after 1 run
  BRO1603  Documentation text should end with a ...    65 files
  BRO1001  Members should be ordered                   33 files
  BRO1401  Use a trailing comma in multi-line in...    33 files
  ...
  ... and 24 more rules
Sample (BRO1603, FFMpegCore.Extensions.Downloader/Exceptions/FFMpegDownloaderException.cs):
  -///     Custom exception for FFMpegDownloader
  +///     Custom exception for FFMpegDownloader.
Sample (BRO1401, FFMpegCore.Examples/Program.cs):
  -            FrameRate = 30 //set source frame rate
  +            FrameRate = 30, // set source frame rate
Full diff: stylebro-preview.patch (5,966 lines)
Done in 1m 2s.
```

Files are counted under every rule `dotnet format --verify-no-changes` reported in them before the fixes; a file with
only whitespace changes counts as IDE0055, one with nothing reported (another fix's side effect) as `other`. The patch
is what the real commands write (byte for byte on FFMpegCore); `git apply stylebro-preview.patch` applies it.

## A. A new project, or no StyleCop

### 1. Add the package

Add it to every project, for example in `Directory.Build.props` at the repository root:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="StyleBro.Analyzers" Version="0.3.0-alpha.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

With central package management, the version goes in `Directory.Packages.props` and the reference has none:

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="StyleBro.Analyzers" Version="0.3.0-alpha.1" />
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

`stylebro-migrate init --diff` shows what the block and the format run in step 3 would change, without writing
anything ([Preview first](#preview-first)).

The block sits between `# BEGIN stylebro-migrate` and `# END stylebro-migrate`; running the command again replaces it.
Put your own settings outside it. To get these rules reported by the build too, add
`<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` to `Directory.Build.props`.

`init` also looks at your code first. When at least three quarters of the private fields start with `_`, it writes
`stylebro_private_field_naming = _camelCase`, so [BRO1303](rules/BRO1303.md) keeps your underscores instead of
renaming every field (generated code, EF Core migrations and submodules don't count; your own `dotnet_naming_rule`
settings win). And when it finds a StyleCop setup (`stylecop.json`, StyleCop rule ids in `.editorconfig`, rulesets or
global configs, a StyleCop.Analyzers reference), it stops without writing and tells you to run
`stylebro-migrate --write` instead: that's [path B](#b-coming-from-stylecop), which turns StyleBro's rules on only where
StyleCop enforced them.

### 3. Run stylebro-migrate format

```
stylebro-migrate format
```

This fixes whitespace, the built-in rules and every StyleBro rule in one go. A second run should change nothing.
Review the diff and commit it. In a large codebase you may not want one big change: see
[Large codebases](#large-codebases).

**Why not plain `dotnet format`?** It also applies every other analyzer's fixes and the compiler's own, and some of
those change behavior or break the build: on one trial repository it added `required` to properties (CS8618) and a
Sonar fix removed `init;` accessors, nine build errors. `stylebro-migrate format` passes `dotnet format` only StyleBro's
rule ids and the built-in rules `init` or the migration turn on (`--diagnostics ...`); whitespace formatting runs as
usual. It also runs once per target framework where needed ([Multi-targeted repositories](#multi-targeted-repositories))
and never touches git submodules. The plain equivalent is `dotnet format --diagnostics` with the ids to fix:

```
dotnet format --diagnostics BRO1001 BRO1505 BRO1601 IDE0055 IDE0036 IDE0065
```

`stylebro-migrate format --all` applies everything, like plain `dotnet format`.

[What to expect on the first run](#what-to-expect-on-the-first-run): solution names, run time, warnings as errors.

### 4. Check it in CI

Run `dotnet format --verify-no-changes --severity warn` after the build: it fails when `dotnet format` would change a
file. Set-up for GitHub Actions and Azure Pipelines: [ci.md](ci.md).

## B. Coming from StyleCop

Still deciding? [StyleBro vs StyleCop](stylebro-vs-stylecop.md) compares the two with measured numbers.

`stylebro-migrate` reads your StyleCop setup (rulesets, global configs, `.editorconfig` files, `stylecop.json`) and
writes StyleBro and SDK settings that enforce the same things, so code StyleCop was happy with stays as it is. A
walk-through on a small project, with real output: [samples/StyleCopMigration](https://github.com/bisforboman/stylebro/blob/main/samples/StyleCopMigration/README.md).

### 1. Dry run

```
stylebro-migrate path/to/repo
```

It prints what it read, the settings it would write, and the StyleCop rules nothing enforces after the switch (dropped
by design, partly covered, or not covered yet). Nothing is changed. To also see what the settings and the format run
would do to the code: `stylebro-migrate path/to/repo --diff` ([Preview first](#preview-first)).

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

### 4. Run stylebro-migrate format

```
stylebro-migrate format
```

Why not plain `dotnet format`: [step 3 of path A](#3-run-stylebro-migrate-format). In a StyleCop-clean repository this should change
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

# Entity Framework Core migrations: generated by 'dotnet ef', regenerated rather than edited
[**/Migrations/**]
generated_code = true
```

EF Core's migration files (`Migrations/*.cs` and the model snapshot) are generated but carry no `<auto-generated>`
header, so without this the first run reformats every one of them (291 files in one trial repository).

StyleBro's rules, the built-in rules (IDE0055, IDE0073, ...) and `dotnet format`'s whitespace pass all skip generated
code, in the build and in `dotnet format`. One side effect: the compiler treats generated files as nullable-oblivious
unless they say `#nullable enable` themselves.

`dotnet_analyzer_diagnostic.severity = none` in that folder isn't enough: a rule turned on by its id
(`dotnet_diagnostic.BRO1001.severity = warning` in the root) wins over it. `stylebro-migrate --write` therefore writes
every replacing rule as `none` into such a folder's `.editorconfig`, and it never edits files inside git submodules
(folders with their own `.git`).

**Files linked from a submodule.** A project can compile files from another folder (`<Compile Include="..\..\ext\Lib\Code.cs" />`,
or a shared `.projitems`). They're checked like the project's own files, but `stylebro-migrate format` never formats
anything inside a git submodule (it passes `--exclude` with every submodule folder), so their warnings stay. Mark the
submodule's folder as generated code too, with a section for its path in the root `.editorconfig` (`[ext/**]`, then
`generated_code = true`).

## Multi-targeted repositories

If any project sets several `<TargetFrameworks>`, plain `dotnet format` doesn't work at all; `stylebro-migrate format`
does. It takes the same arguments and options:

```
stylebro-migrate format
stylebro-migrate format MySolution.sln --verify-no-changes --severity warn
```

Plain `dotnet format` crashes there on the SDK's formatting fix (IDE0055) without writing anything. The command runs
`dotnet format` once per target framework instead; without multi-targeted projects it's one run. A diagnostic
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
