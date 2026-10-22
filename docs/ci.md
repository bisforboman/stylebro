# StyleBro in CI

Two checks keep a repository clean. They overlap on purpose: the build catches what someone forgot to format, and
`dotnet format` proves that the code is exactly what the fixes produce.

1. **The build** reports StyleBro's rules as warnings (and the built-in .NET rules too, with
   `EnforceCodeStyleInBuild`). With warnings as errors, a violation fails the build.
2. **`bro check`** fails (exit code 2) when formatting would change any file. It
   also covers whitespace formatting, which isn't a build warning. Unlike plain `dotnet format --verify-no-changes`, it
   doesn't fail on [findings kept on purpose](kept-findings.md#findings-kept-on-purpose): renames StyleBro's fixes
   leave because they'd break code only the fix can see (a name read by reflection, say). Those stay as build warnings,
   so suppress or [baseline](baseline.md) them where warnings are errors.

Exit codes of `bro check`: **0** clean (nothing to change; kept findings are listed
with their reasons), **2** formatting would change a file (the output says which findings), **3** not verified: nothing
to change in what it checked, but part of the build wasn't checked (a project that didn't load, a missing workload, ...;
each is named, see [Not verified](kept-findings.md#not-verified); IDE fixes held back on an older .NET SDK are listed
but don't count), anything else: `dotnet format` itself failed. A script or an AI agent can loop "run `bro format`, then check" and stop on 0; with
plain `dotnet format --verify-no-changes` such a loop never ends while kept findings exist.

To catch problems before they're committed: [Pre-commit hook](pre-commit.md).

Fixing a failure is the same everywhere: run `bro format` (or `dotnet format`, see
[the notes](#notes) on other analyzers) locally and commit the result.

## Setup

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <!-- Report the built-in .NET rules (IDE0055, IDE0036, ...) on build too. -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-alpha.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Then, once, `bro init --write` (or `bro migrate --write` when coming from StyleCop) to put the
built-in rules' severities into `.editorconfig`. They can't come from the package: `dotnet format` ignores rule
severities in a package's global config, so without them in `.editorconfig` the build would report rules
`dotnet format` doesn't fix.

Prefer `TreatWarningsAsErrors` alone. Don't list StyleBro rules in `<WarningsNotAsErrors>`: plain `dotnet format` then
skips those rules entirely (see [BRO1001](rules/BRO1001.md)). `bro format` and its `--verify-no-changes`
take StyleBro's rules out of that list for their own runs (and say so), so they fix and check them like the build
reports them.

Where the repository formats with CSharpier, `bro format` runs it on the files it changed, and
`bro check` runs CSharpier's check on the C# files it formats too: a separate
`dotnet csharpier check` step stays useful for the other files CSharpier formats (project files, JSON).

## Build every configuration

```
bro check --build
```

After the check, builds the solution in every configuration it lists (`Debug`, `Release`, `Release|x64`, ...; a project:
its `Configurations`, else Debug and Release), every target framework, with `TreatWarningsAsErrors`, as CI does. A
finding of `bro format`'s rules in any of these builds fails the check (exit code 2): that's code no `dotnet format` run
saw, `#if !DEBUG` code for example. Any other error fails only when it is new: the same builds of a copy of git HEAD
(the working tree is never touched) don't have it, compared by file, rule and message (a fix may move the line). Errors
the untouched code has too are counted but don't fail it, and without git every error does. A build that fails like the
untouched code's (a missing workload at the restore, a compile error) checked only what built before the error: the
check then ends "Not verified" (exit code 3), naming the projects a failed one kept from compiling (`Didn't compile: App`). IDE fixes
`format` holds back on this .NET SDK (IDE0049) are only a note and don't change the verdict; when every project sets
`EnforceCodeStyleInBuild` and every file has the rule at warning or error, the check also says the builds checked them.
Tests aren't run. It takes a
build per configuration, two after `bro format` changed files, so it's slow: run it in CI or before a release rather
than after every edit.

## GitHub Actions

```yaml
name: CI
on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-dotnet@v6
        with:
          dotnet-version: 10.0.x

      - run: dotnet build MySolution.sln

      - run: dotnet tool install --global StyleBro.Tool --prerelease

      - name: Code style
        run: bro check MySolution.sln --severity warn --no-restore --report format-report

      - name: Upload what dotnet format would change
        if: failure()
        uses: actions/upload-artifact@v7
        with:
          name: format-report
          path: format-report
```

## Azure Pipelines

```yaml
steps:
  - task: UseDotNet@2
    inputs:
      version: 10.0.x

  - script: dotnet build MySolution.sln
    displayName: Build

  - script: dotnet tool install --global StyleBro.Tool --prerelease
    displayName: Install bro

  - script: bro check MySolution.sln --severity warn --no-restore --report $(Build.ArtifactStagingDirectory)/format-report
    displayName: Code style

  - publish: $(Build.ArtifactStagingDirectory)/format-report
    artifact: format-report
    condition: failed()
```

## Notes

- **`--severity warn`** makes `dotnet format` apply and check everything at warning level, StyleBro's default.
- **`--no-restore`** after a build saves a second restore; `dotnet format` needs a restored solution either way.
- **`--report`** writes `format-report.json`: every file and line `dotnet format` would change and why (rule id or
  `WHITESPACE`), kept findings included. Uploading it on failure shows what to fix without rerunning anything.
- **How `--verify-no-changes` tells kept findings apart:** when `dotnet format --verify-no-changes` reports findings,
  `bro format` formats a temporary copy of the repository (the fixes only run when they may write) and
  checks whether any file changed; the fixes write down which findings they kept and why. The copy is deleted, the
  repository isn't touched. That costs a second format run, only when there are findings.
- **The same result on every OS:** without `end_of_line` in `.editorconfig`, `dotnet format`'s whitespace pass writes
  the OS's line ending into the line breaks it rewrites (after `=>` followed by a comment, for example), so a CRLF file
  checked on Linux, or an LF file on Windows, looked unformatted. `bro format` keeps each file's own
  ending when the file used one throughout, and `--verify-no-changes` ignores findings that only swap it. Setting
  `end_of_line` (`bro init` does when your files agree) picks one ending for every file instead.
- **Without the tool:** plain `dotnet format --verify-no-changes --severity warn` works too, but it fails on kept
  findings. Suppress or baseline them first.
- **Other analyzers and compiler fixes.** Plain `dotnet format --verify-no-changes` also fails on every other analyzer
  package's fixable warnings and on compiler fixes (CS8618's `required`), and locally plain `dotnet format` applies
  them, which can change behavior or break the build. To check and fix only StyleBro's rules and the built-in rules
  `bro init` turns on, name them: `dotnet format --diagnostics BRO1001 BRO1505 ... IDE0055 IDE0036
  --verify-no-changes`. `bro format` (the .NET tool StyleBro.Tool, installed in the examples above)
  builds that list for you, locally and in CI.
- **Only some checks?** `dotnet format whitespace`, `dotnet format style` (built-in .NET rules) and
  `dotnet format analyzers` (StyleBro and other analyzer packages) check one part each, with the same options.
- **How long it takes:** a check loads the whole solution, once per target framework in multi-targeted repositories.
  Orleans (about 200 projects) took 11.5 minutes for `bro check` and 57 minutes
  (about 9 GB of memory) for its first full `format`. Budget the job's timeout accordingly, or check only the projects
  a change touches.
- **Kept findings and `TreatWarningsAsErrors`:** the findings StyleBro's fixes keep on purpose don't fail
  `bro check`, but they are warnings, so they fail a build that treats warnings as
  errors. Suppress or baseline them in the first run ([getting-started.md](kept-findings.md#findings-kept-on-purpose)).
- **CI builds Release:** plain `dotnet format` checks the Debug configuration only, so code under `#if !DEBUG` isn't
  checked while a Release build with warnings as errors reports it. `bro check` also
  checks the other configurations where a project's code compiles differently there
  ([getting-started.md](debug-and-release.md#debug-and-release)).
- **Old-style (non-SDK) projects:** restore with Visual Studio's MSBuild before the check
  (`msbuild -t:restore -p:RestorePackagesConfig=true`) and keep the .NET Framework reference assemblies available:
  [getting-started.md](old-style-projects.md#old-style-non-sdk-projects).
- **A large existing codebase:** commit a [baseline](baseline.md) so CI fails only on new violations. A baseline can't
  hide whitespace formatting; either run `dotnet format whitespace` once, or check only `dotnet format style` and
  `dotnet format analyzers` in CI until you do.
- **Several target frameworks:** use `bro format` instead of `dotnet format`, with the same options:

  ```
  bro format MySolution.sln --verify-no-changes --severity warn
  ```

  `dotnet format` loads one copy of every file per target framework, and the SDK's formatting fix (IDE0055) edits
  each copy on its own: each sees other `#if` code, Roslyn can't merge the results, and `dotnet format` crashes
  without writing anything (Serilog, Newtonsoft.Json; reported upstream as [dotnet/roslyn#86000](https://github.com/dotnet/roslyn/issues/86000) and [dotnet/roslyn#86001](https://github.com/dotnet/roslyn/issues/86001)). Its whitespace pass only formats the first framework's code ([dotnet/sdk#56585](https://github.com/dotnet/sdk/issues/56585)),
  so code behind `#if` stays unformatted while the other frameworks' builds report it. `bro format` runs
  `dotnet format` once per target framework, each time on the projects that target it, loaded for that framework
  alone: nothing to merge, and every framework's `#if` code gets fixed (Serilog: 505 IDE0055 warnings to 0; with all
  StyleBro rules, the second run changes nothing). A repository without multi-targeted projects gets one plain
  `dotnet format` run. The SDK fixes that wrote conflict markers there (IDE0011, IDE0040, IDE0047/IDE0048,
  IDE2000/IDE2002/IDE2003; reported upstream as [dotnet/roslyn#86000](https://github.com/dotnet/roslyn/issues/86000)) are replaced by StyleBro rules and off.

  One limit: code that has to be indented differently for different frameworks, like an `else` inside `#if` followed
  by a block after `#endif`, can't satisfy IDE0055 for all of them; the last framework's run wins and the build
  reports it for the others (Newtonsoft.Json: 32 lines in 4 files). Restructure such code, or suppress IDE0055 there.
