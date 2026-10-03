# StyleBro in CI

Two checks keep a repository clean. They overlap on purpose: the build catches what someone forgot to format, and
`dotnet format` proves that the code is exactly what the fixes produce.

1. **The build** reports StyleBro's rules as warnings (and the built-in .NET rules too, with
   `EnforceCodeStyleInBuild`). With warnings as errors, a violation fails the build.
2. **`dotnet format --verify-no-changes`** fails (exit code 2) when running `dotnet format` would change any file. It
   also covers whitespace formatting, which isn't a build warning.

Fixing a failure is the same everywhere: run `dotnet format` locally and commit the result.

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
    <PackageReference Include="StyleBro.Analyzers" Version="0.1.0-alpha.8" PrivateAssets="all" />
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
- **Only some checks?** `dotnet format whitespace`, `dotnet format style` (built-in .NET rules) and
  `dotnet format analyzers` (StyleBro and other analyzer packages) check one part each, with the same options.
- **A large existing codebase:** commit a [baseline](baseline.md) so CI fails only on new violations. A baseline can't
  hide whitespace formatting; either run `dotnet format whitespace` once, or check only `dotnet format style` and
  `dotnet format analyzers` in CI until you do.
- **Several target frameworks:** `dotnet format` checks every framework's copy of a file. StyleBro's fixes give all
  copies the same text, so `#if` code doesn't cause conflicts; when `dotnet format`'s earlier passes leave the copies
  different, StyleBro gives all of them the first copy's fixed text, and a second run picks up what only another
  copy's `#if` code needed (Newtonsoft.Json, 8 target frameworks: clean after two runs). Several of the SDK's fixes
  aren't safe there: on Serilog and Newtonsoft.Json, IDE0011 (braces; replaced by StyleBro's BRO1514-BRO1516) and IDE0055 (formatting, at warning) crashed
  `dotnet format` (nothing written), and IDE0040 (access modifiers; replaced by BRO1404/BRO1007), IDE0047/IDE0048 (parentheses; IDE0047 replaced by BRO1405) and the blank-line
  rules IDE2000/IDE2002/IDE2003 wrote merge conflict markers. In a repository with multi-targeted projects,
  `stylebro-migrate init` writes the remaining ones as suggestions (shown in the IDE, not fixed by `dotnet format --severity warn`;
  whitespace is still formatted). Turn them back to `warning` only after checking that `dotnet format` handles your
  code.
