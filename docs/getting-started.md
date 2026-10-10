# Getting started

From install to the first `dotnet format` run. Pick the path that fits:

- [Preview first](#preview-first): what either path would change, without changing anything
- [A. A new project, or no StyleCop](#a-a-new-project-or-no-stylecop)
- [B. Coming from StyleCop](#b-coming-from-stylecop)

Both use two packages: **StyleBro.Analyzers** (the rules, referenced by your projects) and **StyleBro.Migrate** (the
`stylebro-migrate` command line tool, used once to write settings):

### Which version

The latest release on nuget.org is **0.4.0-alpha.1**, a prerelease: install the tool with `--prerelease` and reference
that exact version. Everything on this page is in it. To try the code on `main` before the next release, build both
packages from a clone into a local folder (any version string works; `0.4.0-dev.1` here):

```
git clone https://github.com/bisforboman/stylebro
dotnet pack stylebro/src/StyleBro.Package -o stylebro-feed -p:Version=0.4.0-dev.1
dotnet pack stylebro/src/StyleBro.Migrate -o stylebro-feed -p:Version=0.4.0-dev.1
dotnet tool install --global StyleBro.Migrate --add-source stylebro-feed --version 0.4.0-dev.1
```

Then add the folder as a package source in your repository's `nuget.config`
(`<add key="stylebro" value="path/to/stylebro-feed" />`) and use `0.4.0-dev.1` wherever this page says `0.4.0-alpha.1`.

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
  StyleBro.Analyzers wasn't referenced: added StyleBro.Analyzers 0.4.0-alpha.1 to Directory.Build.props for the preview, as docs/getting-started.md says (the patch includes it).
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
    <PackageReference Include="StyleBro.Analyzers" Version="0.4.0-alpha.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

With central package management, the version goes in `Directory.Packages.props` and the reference has none:

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="StyleBro.Analyzers" Version="0.4.0-alpha.1" />
</ItemGroup>

<!-- Directory.Build.props -->
<ItemGroup>
  <PackageReference Include="StyleBro.Analyzers" PrivateAssets="all" />
</ItemGroup>
```

MSBuild imports only the nearest `Directory.Build.props` above a project: if a folder (say `src/`) has its own and
it doesn't import the root one, put the reference there (or in each such file). A reference inside a conditional
`ItemGroup` (`Condition="'$(TargetFramework)' == 'net8.0'"`) reaches only that target framework, and
`<RunAnalyzers>false</RunAnalyzers>` turns all analyzers off where it applies. `--diff` adds the reference to every
`Directory.Build.props` the projects use and warns when StyleBro reported nothing at all.

The package brings StyleBro's rules and its preset: rule severities and formatting options, close to StyleCop's
defaults. Your own `.editorconfig` wins over the preset. To configure everything yourself instead:
`<StyleBroPreset>none</StyleBroPreset>`.

### 2. Turn on the built-in rules

StyleBro relies on some built-in .NET rules (IDE0055 formatting, IDE0036 modifier order, IDE0049 type aliases, ...).
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
`<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` to `Directory.Build.props` (`init` says so when no project
file sets it yet).

#### init also looks at your code

`init` looks at your code first and keeps the conventions it clearly follows. For each setting below it counts both
forms in your C# (generated code, EF Core migrations, vendored folders and submodules don't count):

- **fewer than 3 places** (or none): too few to tell, StyleBro's default stays and nothing is written;
- **3 to 9 places**: followed when they all agree;
- **10 places or more**: followed when one form has at least 75 %;
- **mixed** (no form gets there): the StyleBro rule that enforces the setting is turned off, so the code stays as it is
  (`dotnet_diagnostic.BRO1520.severity = none`; for braces on one-line bodies `csharp_prefer_braces = when_multiline`,
  which allows both).

A followed form that is StyleBro's default needs no line; any other is written into the block. A key your root
`.editorconfig` sets wins, so does a Sonar setup's, and a `dotnet_naming_rule` for private fields decides the field style
(naming rules for interfaces or constants don't).

| Setting | What's counted | Rule | Key |
|---|---|---|---|
| Private field names | `field` or `_field` | [BRO1303](rules/BRO1303.md) | `stylebro_private_field_naming` |
| `{` placement | `{` of a multi-line block on its own line or at the end of the line | SDK formatter (IDE0055) | `csharp_new_line_before_open_brace` |
| `else`/`catch`/`finally` | after `}` on a new line or the same line | SDK formatter (IDE0055) | `csharp_new_line_before_else`, `_catch`, `_finally` |
| Braces | one-line `if`/`else`/`for`/`foreach`/`while`/`using`/`lock` bodies with or without braces | [BRO1514](rules/BRO1514.md)-[BRO1516](rules/BRO1516.md) | `csharp_prefer_braces` |
| Operator placement | binary and `?:` operators at the start or end of a wrapped line | [BRO1520](rules/BRO1520.md) | `dotnet_style_operator_placement_when_wrapping` |
| `=>` placement | `=>` of expression bodies and switch arms where a line wraps | [BRO1521](rules/BRO1521.md) | `stylebro_arrow_placement_when_wrapping` |
| `=` placement | `=` where a line wraps | [BRO1522](rules/BRO1522.md) | `stylebro_equals_placement_when_wrapping` |
| Trailing commas | multi-line initializers, enums and switch expressions | [BRO1401](rules/BRO1401.md) | `stylebro_trailing_comma` |
| Empty strings | `string.Empty` or `""` (outside constant contexts) | [BRO1106](rules/BRO1106.md) | `stylebro_empty_string_style` |
| Null checks | `is null` or `== null` | [BRO1133](rules/BRO1133.md) | `stylebro_null_check_style` |
| Summaries | one-line `<summary>` texts with the tags on their own lines or on one line | [BRO1616](rules/BRO1616.md) | `stylebro_summary_layout` |
| `<inheritdoc/>` | `<inheritdoc/>` or `<inheritdoc />` | [BRO1601](rules/BRO1601.md) | `stylebro_inheritdoc_style` |
| Default values | `default(T)` or `default` | IDE0034 | `csharp_prefer_simple_default_expression` |
| `)` of split lists | after the last item or on its own line | [BRO1110](rules/BRO1110.md) | `stylebro_closing_parenthesis_placement` |
| First item of split lists | on the next line or after `(` | [BRO1107](rules/BRO1107.md) | `stylebro_split_list_first_item` |
| Constructor initializers | `: base(...)`/`: this(...)` on its own line or the constructor's | [BRO1105](rules/BRO1105.md) | `stylebro_constructor_initializer_placement` |
| Constraints | `where` on its own line or the declaration's | [BRO1111](rules/BRO1111.md) | `stylebro_constraint_placement` |
| `new T { ... }` | with or without `()` before an initializer | [BRO1141](rules/BRO1141.md) | `stylebro_object_creation_parentheses` |
| Switch sections | after a blank line or without one | [BRO1526](rules/BRO1526.md) | `stylebro_blank_line_between_switch_sections` |
| Using directives | files with a namespace and their usings on one level: inside it (file-scoped too) or outside; only `inside_namespace` is written, the preset already says `outside_namespace` | [BRO1008](rules/BRO1008.md) | `csharp_using_directive_placement` |
| Arithmetic parentheses | `a + (b * c)` or `a + b * c` | [BRO1406](rules/BRO1406.md) | `dotnet_style_parentheses_in_arithmetic_binary_operators` |

The keys are described in [Settings](configuration.md). `init` prints each verdict (`kept`, `default`, `off`, `mixed`,
`too few`, or `set` when your configuration decides) with the value that applies, then a summary that names the keys
(RealWorld):

```
Conventions in the code (judged from 3 places: all must agree below 10, 75% from 10 on;
  mixed: the rule that enforces the setting is turned off):
  too few  private fields: 2 places -> default stays (camelCase)
  set      '{' of multi-line blocks: 196 of 196 on its own line -> set in .editorconfig (all)
  too few  operators where a line wraps: 1 place -> default stays (beginning_of_line)
  kept     multi-line initializers: 18 of 18 without a trailing comma -> omit
  default  empty strings: 3 of 3 string.Empty -> string_empty
  off      null checks: 7 'is null', 10 '== null' -> BRO1133 is off
  off      split lists: 13 first item on the next line, 17 first item after '(' -> BRO1107 is off
  ...
Summary:
  Kept your style for 5 settings (1 written, 4 already StyleBro's default): stylebro_trailing_comma.
  Turned 2 rules off because your code mixes both forms: BRO1133 (null checks), BRO1107 (split lists).
  To choose later: set the key in .editorconfig, remove its 'severity = none' line, run 'stylebro-migrate format'.
  4 settings had fewer than 3 places to tell, StyleBro's defaults stay: stylebro_private_field_naming,
    dotnet_style_operator_placement_when_wrapping, stylebro_equals_placement_when_wrapping, stylebro_summary_layout.
```

Each written line gets a comment in the block saying why:

```ini
# init: multi-line initializers: without a trailing comma in 18 of 18 places in your code
stylebro_trailing_comma = omit
# init: null checks: your code mixes both forms (7 'is null', 10 '== null'), so BRO1133 is off. To choose one: set stylebro_null_check_style and remove the next line.
dotnet_diagnostic.BRO1133.severity = none
```

#### Changing a detected setting later

Set the key in your own part of `.editorconfig`, outside the block (for example `stylebro_null_check_style =
equality_operator`), remove the rule's `severity = none` line from the block, and run `stylebro-migrate format` to bring
the code in line. The block is rewritten each time `init` runs, but a key you set yourself wins over what it counts: it
then writes neither the setting nor the `severity = none` line.

EF Core migrations (files with `[Migration(...)]`, a `Migration` base class or a `ModelSnapshot`) are written by
`dotnet ef`, so `init` marks their folders `generated_code = true`: formatting and StyleBro leave them alone. Vendored
folders (`vendor`, `vendored`, `third_party`, `thirdparty`, `external`) get the same section.

When it finds a StyleCop setup (`stylecop.json`, StyleCop rule ids in `.editorconfig`, rulesets or
global configs, a StyleCop.Analyzers reference), it stops without writing and tells you to run
`stylebro-migrate --write` instead: that's [path B](#b-coming-from-stylecop), which turns StyleBro's rules on only where
StyleCop enforced them.

With SonarQube or SonarCloud (a `SonarAnalyzer.CSharp` reference, Sonar rule severities in rulesets or configs, or a
quality profile you export and pass with `--sonar-profile profile.xml`), `init` also turns on the StyleBro and .NET rules
that fix what your Sonar rules report (S2325 -> CA1822, S4136 -> overloads kept together, ...) and prints which:
[Coming from SonarQube](migrating.md#coming-from-sonarqube). The same works for `stylebro-migrate --write` in path B.

### 3. Run stylebro-migrate format

```
stylebro-migrate format
```

This fixes whitespace, the built-in rules and every StyleBro rule. One fix can make work for another rule, so the
command runs `dotnet format` again until a run changes no file (at most three runs) and prints what each run changed
(`Run 1: 293 files changed:` and the first 10 of them, `Run 2: 0 files changed, clean.`); `--once` runs it once. It ends
with the findings StyleBro's fixes leave on purpose, each with its reason ([Findings kept on purpose](#findings-kept-on-purpose)),
and exits with 0 when clean, 2 when a run still changed files. When `dotnet format` skips a project because its
references didn't load (a broken restore or package cache), the command names it and says the run is incomplete (for
one target framework only: the code only that framework compiles); `--diff` stops then instead of showing a partial
preview. A folder with several solutions
or projects needs one named (`stylebro-migrate format MySolution.slnx`): the command lists them instead of picking one.
Review the diff and commit it. In a large codebase you may not want one big change: see
[Large codebases](#large-codebases).

**Why not plain `dotnet format`?** It also applies every other analyzer's fixes and the compiler's own, and some of
those change behavior or break the build: on one trial repository it added `required` to properties (CS8618) and a
Sonar fix removed `init;` accessors, nine build errors. `stylebro-migrate format` passes `dotnet format` only StyleBro's
rule ids and the built-in rules `init` or the migration turn on (`--diagnostics ...`); whitespace formatting runs as
usual. It also runs once per target framework where needed ([Multi-targeted repositories](#multi-targeted-repositories))
and never touches git submodules. The plain equivalent is `dotnet format --diagnostics` with the ids to fix:

```
dotnet format --diagnostics BRO1001 BRO1505 BRO1601 IDE0055 IDE0036 BRO1008
```

`stylebro-migrate format --all` applies everything, like plain `dotnet format`.

[What to expect on the first run](#what-to-expect-on-the-first-run): solution names, run time, warnings as errors.

### 4. Check it in CI

Run `stylebro-migrate format --verify-no-changes --severity warn` after the build: it changes nothing and fails (exit
code 2) when formatting would change a file. Findings kept on purpose don't fail it; plain
`dotnet format --verify-no-changes` fails on those too, so a script or an AI agent that loops "until clean" never gets
there with it. Set-up for GitHub Actions and Azure Pipelines: [ci.md](ci.md).

`init --write` also puts a short StyleBro section into `AGENTS.md` for AI coding agents (`--no-agents-md` leaves it
alone). For scripts and agents there are `format --files` (just the files an edit touched) and `--json`:
[Scripts and AI agents](agents.md).

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
  stay after `dotnet format`: see [Findings kept on purpose](#findings-kept-on-purpose).

## Findings kept on purpose

Every StyleBro finding has a fix, but a fix can see more than the analyzer: the whole solution, other projects
included. When a change would break something only the fix can see, it leaves the finding, the warning stays, and the
fix offers no code action for it. That's the one exception to "`dotnet format` fixes everything StyleBro reports",
and it's on purpose. The reasons, per finding:

- the name is in a string anywhere in the solution (reflection like `GetField("_count")`, serialized names, test JSON):
  the rename would compile but break at run time;
- the name is in a `nameof(...)` elsewhere, whose text would change with it (an HTTP header name, a log key);
- another member would hide the new name, or the new name already means something else where the name is used;
- a use is in generated code (a source generator, a Razor page, a designer file), which a tool writes again;
- a related override or implementation is public API, or also implements a member that isn't renamed;
- the rename would add compile errors (tuple element names), or the namespace is also defined outside the solution.

`stylebro-migrate format` lists them after its last run:

```
Run 2: 0 files changed, clean.
Clean: 1 finding kept on purpose (listed below). StyleBro's fixes leave these: renaming by hand breaks what the check protects. Check those uses first, or suppress or baseline them (...).
  src/Counter.cs(5,17): BRO1303 Rename '_count' to 'count'. Kept: the name is in a string in the solution (reflection, serialization): renaming would compile but break at run time.
```

What to do with one: **don't rename by hand to silence it** (that's what breaks the reflection or the generated code).
Check the uses the reason names; if they can change too, rename both by hand. Otherwise suppress it with a reason
(`#pragma warning disable BRO1303 // read by reflection in Tests`), or record it in the [baseline](baseline.md).

Exit codes of `stylebro-migrate format`, for CI and agents:

| Exit code | Plain run | `--verify-no-changes` |
|---|---|---|
| 0 | clean: the last run changed nothing (kept findings don't count) | formatting would change nothing; only kept findings are left |
| 2 | still changing after three runs | formatting would change a file |
| other | `dotnet format` failed (load or build errors) | the same |

`--verify-no-changes` changes nothing: when `dotnet format --verify-no-changes` reports findings, it formats a temporary
copy of the repository to see whether any file would change and which findings the fixes keep, then deletes the copy.
Plain `dotnet format --verify-no-changes` can't tell the two apart and fails on kept findings.

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
