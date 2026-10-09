# StyleBro in CI

Two checks keep a repository clean. They overlap on purpose: the build catches what someone forgot to format, and
`dotnet format` proves that the code is exactly what the fixes produce.

1. **The build** reports StyleBro's rules as warnings (and the built-in .NET rules too, with
   `EnforceCodeStyleInBuild`). With warnings as errors, a violation fails the build.
2. **`dotnet format --verify-no-changes`** fails (exit code 2) when running `dotnet format` would change any file. It
   also covers whitespace formatting, which isn't a build warning.

Fixing a failure is the same everywhere: run `stylebro-migrate format` (or `dotnet format`, see
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
    <PackageReference Include="StyleBro.Analyzers" Version="0.2.0-alpha.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Then, once, `stylebro-migrate init --write` (or `stylebro-migrate --write` when coming from StyleCop) to put the
built-in rules' severities into `.editorconfig`. They can't come from the package: `dotnet format` ignores rule
severities in a package's global config, so without them in `.editorconfig` the build would report rules
`dotnet format` doesn't fix.

Prefer `TreatWarningsAsErrors` alone. Don't list StyleBro rules in `<WarningsNotAsErrors>`: `dotnet format` then skips
those rules entirely (see [BRO1001](rules/BRO1001.md)).

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

      - name: Code style
        run: dotnet format MySolution.sln --verify-no-changes --severity warn --no-restore --report format-report

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

  - script: dotnet format MySolution.sln --verify-no-changes --severity warn --no-restore --report $(Build.ArtifactStagingDirectory)/format-report
    displayName: Code style

  - publish: $(Build.ArtifactStagingDirectory)/format-report
    artifact: format-report
    condition: failed()
```

## Notes

- **`--severity warn`** makes `dotnet format` apply and check everything at warning level, StyleBro's default.
- **`--no-restore`** after a build saves a second restore; `dotnet format` needs a restored solution either way.
- **`--report`** writes `format-report.json`: every file and line `dotnet format` would change and why (rule id or
  `WHITESPACE`). Uploading it on failure shows what to fix without rerunning anything.
- **Other analyzers and compiler fixes.** Plain `dotnet format --verify-no-changes` also fails on every other analyzer
  package's fixable warnings and on compiler fixes (CS8618's `required`), and locally plain `dotnet format` applies
  them, which can change behavior or break the build. To check and fix only StyleBro's rules and the built-in rules
  `stylebro-migrate init` turns on, name them: `dotnet format --diagnostics BRO1001 BRO1505 ... IDE0055 IDE0036
  --verify-no-changes`. `stylebro-migrate format` (the .NET tool StyleBro.Migrate) builds that list for you and is what
  [getting-started.md](getting-started.md#3-run-stylebro-migrate-format) recommends locally; in CI it needs
  `dotnet tool install --global StyleBro.Migrate --prerelease` first.
- **Only some checks?** `dotnet format whitespace`, `dotnet format style` (built-in .NET rules) and
  `dotnet format analyzers` (StyleBro and other analyzer packages) check one part each, with the same options.
- **A large existing codebase:** commit a [baseline](baseline.md) so CI fails only on new violations. A baseline can't
  hide whitespace formatting; either run `dotnet format whitespace` once, or check only `dotnet format style` and
  `dotnet format analyzers` in CI until you do.
- **Several target frameworks:** use `stylebro-migrate format` instead of `dotnet format`, with the same options:

  ```
  stylebro-migrate format MySolution.sln --verify-no-changes --severity warn
  ```

  `dotnet format` loads one copy of every file per target framework, and the SDK's formatting fix (IDE0055) edits
  each copy on its own: each sees other `#if` code, Roslyn can't merge the results, and `dotnet format` crashes
  without writing anything (Serilog, Newtonsoft.Json). Its whitespace pass only formats the first framework's code,
  so code behind `#if` stays unformatted while the other frameworks' builds report it. `stylebro-migrate format` runs
  `dotnet format` once per target framework, each time on the projects that target it, loaded for that framework
  alone: nothing to merge, and every framework's `#if` code gets fixed (Serilog: 505 IDE0055 warnings to 0; with all
  StyleBro rules, the second run changes nothing). A repository without multi-targeted projects gets one plain
  `dotnet format` run. The SDK fixes that wrote conflict markers there (IDE0011, IDE0040, IDE0047/IDE0048,
  IDE2000/IDE2002/IDE2003) are replaced by StyleBro rules and off.

  One limit: code that has to be indented differently for different frameworks, like an `else` inside `#if` followed
  by a block after `#endif`, can't satisfy IDE0055 for all of them; the last framework's run wins and the build
  reports it for the others (Newtonsoft.Json: 32 lines in 4 files). Restructure such code, or suppress IDE0055 there.
