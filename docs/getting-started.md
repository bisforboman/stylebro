# Getting started

From install to the first `dotnet format` run. Pick the path that fits:

- [Preview first](#preview-first): what either path would change, without changing anything
- [A. A new project, or no StyleCop](#a-a-new-project-or-no-stylecop)
- [B. Coming from StyleCop](#b-coming-from-stylecop)

Both use two packages: **StyleBro.Analyzers** (the rules, referenced by your projects) and **StyleBro.Tool** (the
`bro` command line tool: settings, formatting and checks):

### Which version

The latest release on nuget.org is **0.8.0-alpha.1**, a prerelease: install the tool with `--prerelease` and reference
that exact version. This page describes `main`, which matched that release when it shipped; what changed since then
ships in the next one and is listed in [changes/](https://github.com/bisforboman/stylebro/tree/main/changes).
To try the code on `main` before the next release, build both
packages from a clone into a local folder (any version string works; `0.9.0-dev.1` here):

```
git clone https://github.com/bisforboman/stylebro
dotnet pack stylebro/src/StyleBro.Package -o stylebro-feed -p:Version=0.9.0-dev.1
dotnet pack stylebro/src/StyleBro.Migrate -o stylebro-feed -p:Version=0.9.0-dev.1
dotnet tool install --global StyleBro.Tool --add-source stylebro-feed --version 0.9.0-dev.1
```

Then add the folder as a package source in your repository's `nuget.config` and use `0.9.0-dev.1` wherever this page
says `0.8.0-alpha.1`. When the `nuget.config` has a `<packageSourceMapping>`, the new source needs a mapping too: NuGet
only looks for a package in the sources mapped to it, so the source alone gives NU1102 (package not found):

```xml
<configuration>
  <packageSources>
    <add key="stylebro" value="path/to/stylebro-feed" />
  </packageSources>
  <packageSourceMapping>
    <!-- the mappings already there stay -->
    <packageSource key="stylebro">
      <package pattern="StyleBro.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

## Preview first

Before changing anything, `bro preview` (the commands' `--diff`) shows what a path would do to your code. It works
on all three commands:

```
bro preview init                  # path A: init's settings, then format
bro preview migrate path/to/repo  # path B: the migration's settings (--write), then format
bro preview format                # only the format run
```

The preview copies the repository to a temporary folder (git's tracked and untracked files, not the ignored ones; the
working trees of submodules too), runs the command there, then `bro format` until a run changes nothing,
and deletes the copy. Your repository isn't touched. If it doesn't reference StyleBro.Analyzers yet, the copy gets the
reference from [step 1](#1-add-the-package) (the tool's own version) and the output says so. It prints a summary and
writes the full diff to `.git/stylebro-preview.patch` in the repository's git folder, where it isn't an untracked file
(outside a git repository: the temp folder; `--diff=other.patch` names another file). Where format's last run leaves part
of the build unchecked, the preview ends as format does: the `Not checked:` lines, "Not verified" and exit code 3
([Not verified](kept-findings.md#not-verified)).
`--keep` keeps the copy and prints where it is; `--all` passes on to format; with several solutions or projects in the
folder, `--project MySolution.sln` names the one to format. `--no-restore` skips the restore in the copy: it takes the
repository's own restore output, so run `dotnet restore` first (it's ignored when the preview adds the package, which
needs a restore). Where the preview adds the reference under central package management: next to StyleCop's (a
`GlobalPackageReference` gets a `GlobalPackageReference`, in the same `Directory.Packages.props`), else a version-less
`PackageReference` with the version in the `Directory.Packages.props` that applies (also one that a folder's
`Directory.Packages.props` imports). FFMpegCore, `bro preview init`:

```text
Previewing 'bro init --write' and 'bro format' on a copy; nothing in C:\src\FFMpegCore changes.
Copying the repository to C:\Users\me\AppData\Local\Temp\stylebro-preview-fmsw0dps.3qa\FFMpegCore... 0s
Settings: 31 lines in .editorconfig; 3 lines in Directory.Build.props (StyleBro.Analyzers)
  StyleBro.Analyzers wasn't referenced: added StyleBro.Analyzers 0.8.0-alpha.1 to Directory.Build.props for the preview, as docs/getting-started.md says (the patch includes it).
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
Full diff: .git\stylebro-preview.patch (5,966 lines)
Done in 1m 2s.
```

Files are counted under every rule `dotnet format --verify-no-changes` reported in them before the fixes; a file with
only whitespace changes counts as IDE0055, one with nothing reported (another fix's side effect) as `other`. The patch
is what the real commands write (byte for byte on FFMpegCore); `git apply .git/stylebro-preview.patch` applies it.
That includes the [baseline](kept-findings.md#findings-kept-on-purpose) with the findings the fixes keep on purpose, which the summary
lists (`Baseline (in the patch): ...`). Files a project compiles from outside the repository (a sibling repository's
`..\..\other\*.cs`) are copied next to the copy, so its code compiles there as it does in yours. Where such files are
missing (the other repository isn't checked out), `format` says so (`Missing: ...`) and leaves out IDE0005, which would
remove the `using` directives the missing code needs.

## A. A new project, or no StyleCop

### 1. Add the package

Add it to every project, for example in `Directory.Build.props` at the repository root:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-alpha.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

With central package management, the version goes in `Directory.Packages.props` and the reference has none:

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="StyleBro.Analyzers" Version="0.8.0-alpha.1" />
</ItemGroup>

<!-- Directory.Build.props -->
<ItemGroup>
  <PackageReference Include="StyleBro.Analyzers" PrivateAssets="all" />
</ItemGroup>
```

A project that turns central package management off itself (`<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>`,
often an analyzer or source generator project) fails its restore with the version-less reference (NU1015). Give such
projects a reference with a version, next to the version-less one:

```xml
<!-- Directory.Build.props -->
<ItemGroup>
  <PackageReference Include="StyleBro.Analyzers" PrivateAssets="all" Condition="'$(ManagePackageVersionsCentrally)' == 'true'" />
  <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-alpha.1" PrivateAssets="all" Condition="'$(ManagePackageVersionsCentrally)' != 'true'" />
</ItemGroup>
```

MSBuild imports only the nearest `Directory.Build.props` above a project: if a folder (say `src/`) has its own and
it doesn't import the root one, put the reference there (or in each such file). If it does import it (`GetPathOfFileAbove`,
or a property like Orleans' `$(_ParentDirectoryBuildPropsPath)`), the root's reference is enough: a second one fails the
restore (NU1504, duplicate reference). A reference inside a conditional
`ItemGroup` (`Condition="'$(TargetFramework)' == 'net8.0'"`) reaches only that target framework, and
`<RunAnalyzers>false</RunAnalyzers>` turns all analyzers off where it applies. `--diff` adds the reference to every
`Directory.Build.props` the projects use and names the projects whose restore output has no StyleBro.Analyzers (a
code base that already follows every rule reports nothing, and that's fine).

With lock files (`<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`), the next restore adds StyleBro.Analyzers
to every `packages.lock.json`: commit them with the reference. Where `<RestoreLockedMode>true</RestoreLockedMode>` is on
(always, or in CI), that restore fails instead (NU1004, "the package references have changed"): update the lock files
once with locked mode off, `dotnet restore -p:RestoreLockedMode=false`, and commit them. `--diff` does this in its copy
by itself, and its patch shows the updated lock files.

Two things that get in the way of a package from a local feed (trying a build before it's on nuget.org):

- A `<RestoreSources>` property (often in `Directory.Build.props`) replaces the sources of every `nuget.config`: a feed
  added there is never asked, and the restore fails with NU1101/NU1102. Add the feed to `RestoreSources` too
  (`<RestoreSources>$(RestoreSources);C:\feed</RestoreSources>`), or use `RestoreAdditionalProjectSources`.
- Projects a test run generates inside the repository (say under `artifacts/` or a test's temp folder) inherit the root
  `Directory.Build.props`, and with it the StyleBro reference, which their own restore may not find. Leave them out with a
  condition on the reference, e.g. `Condition="!$(MSBuildProjectDirectory.Contains('artifacts'))"`, or give that folder a
  `Directory.Build.props` of its own that doesn't import the root one.

The package brings StyleBro's rules and its preset: rule severities and formatting options, close to StyleCop's
defaults. Your own `.editorconfig` wins over the preset. The preset has a version, and `bro init --write`
(step 2) pins it in `Directory.Build.props` (`<StyleBroPreset>1.0</StyleBroPreset>`), so a later package upgrade adds no
new rules until you raise it ([versioning](versioning.md#new-rules)). To configure everything yourself instead:
`<StyleBroPreset>none</StyleBroPreset>`.

### 2. Turn on the built-in rules

StyleBro relies on some built-in .NET rules (IDE0055 formatting, IDE0036 modifier order, IDE0049 type aliases, ...).
Their severities have to be in `.editorconfig`, because `dotnet format` ignores rule severities in a package's preset.
In the repository root:

```
bro init            # shows the block
bro init --write    # adds it to .editorconfig (created if missing)
```

`bro preview init` shows what the block and the format run in step 3 would change, without writing
anything ([Preview first](#preview-first)).

The block sits between `# BEGIN stylebro-migrate` and `# END stylebro-migrate`; running the command again replaces it.
Put your own settings outside it. To get these rules reported by the build too, add
`<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` to `Directory.Build.props` (`init` says so when no project
file sets it yet).

An `.editorconfig` that silences analyzers in bulk (`dotnet_analyzer_diagnostic.severity = silent`, or a category's
severity below warning) would hide StyleBro's rules from `dotnet format`, while the build still reports them at the
preset's severities. `init` then writes each rule the preset turns on by its id, so both see the same rules; a rule
your `.editorconfig` sets by id stays as you set it. `bro format` never says clean while such a bulk setting
hides rules from it. The built-in rules are another matter: where your `.editorconfig` turns the Style category off (or
every analyzer, without a Style entry), the block leaves out the ones it would turn on by id (IDE0055, IDE0036, IDE0049),
so your setting decides them, and `format` skips the whitespace pass.

Your own path sections (`[src/**/*.cs]` with `dotnet_diagnostic.IDE0055.severity = none`) come before the block, and the
block's `[*.cs]` lines would win over them: `init` repeats the keys they set otherwise in the same sections at the end
of the block, and says so.

A folder whose own `.editorconfig` turns every analyzer off (`dotnet_analyzer_diagnostic.severity = none` or `silent`,
with or without `root = true`; test samples, for example) is left alone instead: `init` writes StyleBro's rules, IDE0055
and the other built-in rules it manages as `none` by id there (a rule's own entry wins over the bulk one, in the
preset as in the root block), so the build reports nothing there and `format` doesn't touch those files, not even their
whitespace. A rule that folder's `.editorconfig` sets by id stays as you set it. These lines go into a block between
`# BEGIN stylebro-folder` and `# END stylebro-folder` (in a `root = true` folder: its `stylebro-migrate` block, with
nothing else in it). Your own IDE0055 severity (say `suggestion`) is kept everywhere: the block doesn't set it then, and
`format` still formats whitespace, except where your severity is `none` (by id, for the Style category or in bulk):
`format` leaves the whitespace of those files alone, as it does where a block turns IDE0055 off.

`init` also looks at your code first and keeps the conventions it clearly follows (a trailing comma, `_`
fields, `is null`, ...); each line it writes says why. How it counts, what it keeps and how to change a
detected setting later: [What init keeps from your code](init-conventions.md).

EF Core migrations (files with `[Migration(...)]`, a `Migration` base class or a `ModelSnapshot`) are written by
`dotnet ef`, so `init` marks their folders `generated_code = true`: formatting and StyleBro leave them alone. Vendored
folders (`vendor`, `vendored`, `third_party`, `thirdparty`, `external`, or a folder with a license of another copyright
holder than the root's) get the same section.

When it finds a StyleCop setup (`stylecop.json`, StyleCop rule ids in `.editorconfig`, rulesets or
global configs, a StyleCop.Analyzers reference), it stops without writing and tells you to run
`bro migrate --write` instead: that's [path B](#b-coming-from-stylecop), which turns StyleBro's rules on only where
StyleCop enforced them.

With SonarQube or SonarCloud (a `SonarAnalyzer.CSharp` reference, Sonar rule severities in rulesets or configs, or a
quality profile you export and pass with `--sonar-profile profile.xml`), `init` also turns on the StyleBro and .NET rules
that fix what your Sonar rules report (S927 -> BRO1313, S4136 -> overloads kept together, ...) and prints which:
[Coming from SonarQube](migrating.md#coming-from-sonarqube). The same works for `bro migrate --write` in path B.

### 3. Run bro format

```
bro format
```

This fixes whitespace, the built-in rules and every StyleBro rule. One fix can make work for another rule, so the
command runs `dotnet format` again until a run changes no file (at most three runs) and prints what each run changed
(`Run 1: 293 files changed:` and the first 10 of them, `Run 2: 0 files changed, clean.`); `--once` runs it once, with the same report and the findings kept in that run. It ends
with the findings StyleBro's fixes leave on purpose, each with its reason ([Findings kept on purpose](kept-findings.md#findings-kept-on-purpose)),
and exits with 0 when clean, 2 when a run still changed files. When `dotnet format` skips a project because its
references didn't load (a broken restore or package cache), the command names it and says the run is incomplete (for
one target framework only: the code only that framework compiles); `--diff` stops then instead of showing a partial
preview, and the command ends "Not verified" (exit code 3) instead of "Clean", naming what wasn't checked
([Not verified](kept-findings.md#not-verified)). When no project has StyleBro.Analyzers in its restore output (`obj/project.assets.json`), the command stops
before `dotnet format` runs: without the analyzers it would fix only whitespace and the built-in rules and report the
code clean. Projects without it next to ones with it are named (`Without StyleBro: ...`). The command restores once
before the runs; that restore skips NuGet's audit (`-p:NuGetAudit=false`): vulnerability warnings (NU1901-NU1904) are
the build's business, and with `TreatWarningsAsErrors` they failed every run. It skips the restore when the restore
output is current: every project's `obj/project.assets.json` lists StyleBro.Analyzers, the restore succeeded, the
packages it expects are in the package folder, and nothing it depends on changed since (the project files, the
`.props`/`.targets`, `Directory.Packages.props`, `nuget.config` and `global.json` files of the repository and the folders
above it, your user `NuGet.Config`). `--restore` restores anyway (a package replaced under the same version, a changed
package source), `--no-restore` never restores. `--timings` prints how long each phase took at the end (evaluating the
projects, the restore, each check and fix run, CSharpier, the builds `--verify-no-changes` makes). A solution filter (`.slnf`) works like
its solution, with only its projects. A solution with several filters of it next to it (Steeltoe's
`Steeltoe.All.slnx` and its seven `.slnf` parts) counts as that solution; the command says how to name a filter instead.
A folder with several solutions or projects needs one named (`bro format MySolution.slnx`): the command lists them instead of picking one.
With none at the root, it looks for a Traversal project there (`dirs.proj` with `Sdk="Microsoft.Build.Traversal"`,
which `dotnet format` can't load: `format` runs on a temporary solution with the projects it lists), then for solutions
one folder down (`src/MySolution.slnx`): the only one is used, and the command says so (`Using src/MySolution.slnx`);
several are listed, as at the root. `baseline` and `--diff` find them the same way.
If `dotnet format` crashes, the command names the projects of that run, and when the crash came after some files were
written, lists every file it changed (`Run 1 failed after changing 12 files:`).
A solution in a folder of its own (`solutions/App.slnx` listing `../src/...`) works too: the command runs from the
folder that holds all its projects, and the files of the repository they compile from above their folders
(`<Compile Include="..\..\lib\*.cs" />`), which it formats like the rest. Where the repository enforces IDE0005 (unnecessary usings, by id, its Style category
or the bulk severity), the command fixes it too: other fixes (`UInt32` to `uint`) can leave a using unused. After the
runs it names the generated files git tracks in the projects it changed (`*.g.cs`, `*.Designer.cs`, a T4 template's
output, files with an `<auto-generated>` header): `X is generated and checked in: rebuild and commit it.` Where
CSharpier formats the repository, it ends with a reminder to run CSharpier.
A source generator in the solution (a project referenced with `OutputItemType="Analyzer"`) is built first when its
output is missing (`Generators: building Gen first`), so the generated code is part of the
checks: without it, a rename of a member that code uses broke the build. `--verify-no-changes`, `--diff` and
`baseline` do the same.
Visual Basic projects are left alone: `format` and `--verify-no-changes` run on the C# projects only (`Visual Basic:
...`), and a C# member a `.vb` file uses keeps its name (a kept finding).
Review the diff and commit it. In an established codebase, land that commit when few pull requests are open (they
will conflict with it), and add its hash to `.git-blame-ignore-revs` so `git blame` skips it (GitHub reads that file
too; locally: `git config blame.ignoreRevsFile .git-blame-ignore-revs`). In a large codebase you may not want one big
change: see [Large codebases](#large-codebases).

**Why not plain `dotnet format`?** It also applies every other analyzer's fixes and the compiler's own, and some of
those change behavior or break the build: on one trial repository it added `required` to properties (CS8618) and a
Sonar fix removed `init;` accessors, nine build errors. `bro format` passes `dotnet format` only StyleBro's
rule ids and the built-in rules `init` or the migration turn on (`--diagnostics ...`); whitespace formatting runs as
usual. It also runs once per target framework where needed ([Multi-targeted repositories](multi-targeting.md#multi-targeted-repositories))
and never touches git submodules. Where the repository uses xunit, it also applies
[xUnit's assertion fixes](xunit.md). The plain equivalent is `dotnet format --diagnostics` with the ids to fix:

```
dotnet format --diagnostics BRO1001 BRO1505 BRO1601 IDE0055 IDE0036 BRO1008
```

`bro format --all` applies everything, like plain `dotnet format`.

[What to expect on the first run](#what-to-expect-on-the-first-run): solution names, run time, warnings as errors.

### 4. Check it in CI

Run `bro check --severity warn` after the build: it changes nothing and fails (exit
code 2) when formatting would change a file. Findings kept on purpose don't fail it; plain
`dotnet format --verify-no-changes` fails on those too, so a script or an AI agent that loops "until clean" never gets
there with it. Set-up for GitHub Actions and Azure Pipelines: [ci.md](ci.md). `init` and the migration
name the workflow lines (`.github/workflows`, `azure-pipelines.yml`) that still run plain `dotnet format --verify-no-changes`.
To check the staged files before each commit: [Pre-commit hook](pre-commit.md).

With `--agents-md`, `init --write` also puts a short StyleBro section into `AGENTS.md` for AI coding
agents (opt-in). For scripts and agents there are `format --files` (just the files an edit touched) and `--json`:
[Scripts and AI agents](agents.md).

## B. Coming from StyleCop

Still deciding? [StyleBro vs StyleCop](stylebro-vs-stylecop.md) compares the two with measured numbers.

`bro migrate` reads your StyleCop setup (rulesets, global configs, `.editorconfig` files, `stylecop.json`) and
writes StyleBro and SDK settings that enforce the same things, so code StyleCop was happy with stays as it is. A
walk-through on a small project, with real output: [samples/StyleCopMigration](https://github.com/bisforboman/stylebro/blob/main/samples/StyleCopMigration/README.md).

### 1. Dry run

```
bro migrate path/to/repo
```

It prints what it read, the settings it would write, and the StyleCop rules nothing enforces after the switch (dropped
by design, partly covered, or not covered yet). Nothing is changed. To also see what the settings and the format run
would do to the code: `bro preview migrate path/to/repo` ([Preview first](#preview-first)).

### 2. Write the settings

```
bro migrate path/to/repo --write
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
can stay; StyleBro ignores them. `stylecop.json` can also go, with its `<AdditionalFiles>` item: the block
records its settings for later runs.

### 4. Run bro format

```
bro format
```

Why not plain `dotnet format`: [step 3 of path A](#3-run-bro-format). In a StyleCop-clean repository this should change
little or nothing. Look at what it did change: usually code StyleCop
missed, or a rule where StyleBro deliberately differs ([differences-from-stylecop.md](differences-from-stylecop.md)).
Then check the report from step 1 for rules your team relied on that nothing enforces any more. Details:
[migrating.md](migrating.md). CI: [ci.md](ci.md).

## The Microsoft style (optional)

For code written like dotnet/runtime's (`_camelCase` and `s_camelCase` fields, no `this.`, braces optional on one-line
bodies), `bro init --style microsoft --write` puts that style on top of the preset instead of StyleCop's
defaults; your code's own conventions still win, apart from private static fields: they get the style's `s_` unless
your code clearly has none (the report says how many get it). Lines an earlier `init` wrote that the style says too
are dropped. Plain `init` suggests it when your code looks like that. What it sets:
[styles.md](styles.md).

## Newer C# and APIs (optional)

To also rewrite code into newer C# and newer APIs (target-typed `new()`, collection expressions, file-scoped
namespaces, `ArgumentNullException.ThrowIfNull`, ...) with the SDK's own rules, after either path:

```
bro init --modernize --write
dotnet format
```

In multi-targeted projects, StyleBro's multi-target guard hides the newer-API rules where a target framework lacks the
API, and the newer-C# rules are suggestions unless the project sets `LangVersion`. Tiers, examples and the rules left out:
[modernizing.md](modernizing.md).

## What to expect on the first run

- **Name the solution when the root has several.** With more than one project or solution file in the folder (say
  `Build.csproj` next to `MySolution.slnx`), `dotnet format` stops with MSB1011 ("more than one project or solution
  file"). Pass the one to format: `dotnet format MySolution.slnx` (the same for `bro format`).
- **Multi-targeted repositories take longer.** `bro format` runs `dotnet format` once per target
  framework, and each run loads the solution again. That's minutes, not seconds: Dapper, which builds in 13 seconds,
  took 345 seconds to format.
- **`TreatWarningsAsErrors` fails the first build** until everything is fixed or recorded: StyleBro's findings are
  warnings, so they become errors. Run `dotnet format` (or `bro format`) before building, or record what's
  left in a [baseline](baseline.md).
- **Some renames are left to you.** The naming rules don't rename a member whose name also appears in a string
  (reflection, serialized names, `DebuggerDisplay`): the rename would compile but break at run time. Those warnings
  stay after `dotnet format`: see [Findings kept on purpose](kept-findings.md#findings-kept-on-purpose).

## Findings kept on purpose

A fix that would break something only it can see (reflection, serialized names, generated code) leaves its finding
on purpose, and `bro format` lists each with its reason: [Findings kept on purpose](kept-findings.md).

## Excluding vendored code

Mark code you don't own as generated code in the root `.editorconfig`, so formatting and StyleBro leave it alone:
[Excluding vendored code](vendored-code.md).

## Multi-targeted repositories

With several `<TargetFrameworks>`, plain `dotnet format` doesn't work; `bro format` does, once per target framework:
[Multi-targeted repositories](multi-targeting.md).

## Debug and Release

Code under `#if !DEBUG` is fixed and checked too: `bro format` also runs a project's other configurations where its
code compiles differently: [Debug and Release](debug-and-release.md).

## Old-style (non-SDK) projects

Projects in the old format, usually with `packages.config`, work with `bro format` after a few extra steps:
[Old-style (non-SDK) projects](old-style-projects.md).

## Large codebases

If `dotnet format` would change too much at once, record today's violations in a baseline instead:

```
bro baseline                            # in the folder with your solution
bro baseline --project MySolution.sln   # or name it
```

This writes `stylebro.baseline`; commit it. The build, the IDE and `dotnet format` then ignore those violations, and
only new code has to follow the rules. A baseline can't hide whitespace formatting. Details: [baseline.md](baseline.md).

**How long a full run takes.** `format` loads the whole solution once per run, and in a multi-targeted repository once
per target framework. On Orleans (about 200 projects, several target frameworks) the first `bro format`
took 57 minutes and about 9 GB of memory, and `format --verify-no-changes` 11.5 minutes. A repository that size with
`TreatWarningsAsErrors` is best started with a baseline: the build passes right away, and the backlog can be fixed a
project at a time (`bro format path/to/Project.csproj`) or a few files at a time (`--files`).
