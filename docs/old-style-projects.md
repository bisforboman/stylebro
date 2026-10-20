# Old-style (non-SDK) projects

Projects in the old format (`<Project ToolsVersion="15.0" ...>` without `Sdk=`, usually .NET Framework with
`packages.config`) work with `bro format`, with a few extra steps:

- **Restore with Visual Studio's MSBuild first**, from a Developer Command Prompt:

  ```
  msbuild -t:restore -p:RestorePackagesConfig=true
  ```

  `dotnet restore` can't restore `packages.config`, and for the other old-style projects it writes restore output that
  Visual Studio's MSBuild then rejects ("Your project file doesn't list 'win' as a RuntimeIdentifier"). So `format`
  restores only the SDK-style projects when it finds old-style ones, and says so (the `-p:RestorePackagesConfig=true`
  part only matters with `packages.config`). If a build complains like that anyway (something ran
  `dotnet restore`), run the MSBuild restore again.
- **Reference assemblies.** `dotnet format` loads the projects with the .NET SDK, which needs the .NET Framework
  reference assemblies: without them every project is skipped ("Required references did not load"). Install the
  targeting pack (the .NET Framework Developer Pack, or Visual Studio's ".NET Framework 4.x targeting pack"), or use the
  `Microsoft.NETFramework.ReferenceAssemblies.net462` NuGet package (the version your projects target) and point
  MSBuild at it with an environment variable, which reaches both `msbuild` and `dotnet format`:
  `set TargetFrameworkRootPath=%NUGET_PACKAGES%\microsoft.netframework.referenceassemblies.net462\1.0.3\build`.
- **Install StyleBro.Analyzers the packages.config way**, like StyleCop was: Visual Studio's package manager
  (or `Install-Package StyleBro.Analyzers` in its console) writes all three parts, or add them by hand. In
  `packages.config`:

  ```xml
  <package id="StyleBro.Analyzers" version="0.8.0-alpha.1" targetFramework="net462" developmentDependency="true" />
  ```

  In the project (paths relative to it; the `packages` folder is next to the solution):

  ```xml
  <ItemGroup>
    <Analyzer Include="..\packages\StyleBro.Analyzers.0.8.0-alpha.1\analyzers\dotnet\cs\StyleBro.Analyzers.dll" />
    <Analyzer Include="..\packages\StyleBro.Analyzers.0.8.0-alpha.1\analyzers\dotnet\cs\StyleBro.CodeFixes.dll" />
  </ItemGroup>
  <!-- at the end, after the Microsoft.CSharp.targets import: the preset and the baseline -->
  <Import Project="..\packages\StyleBro.Analyzers.0.8.0-alpha.1\build\StyleBro.Analyzers.targets" Condition="Exists('..\packages\StyleBro.Analyzers.0.8.0-alpha.1\build\StyleBro.Analyzers.targets')" />
  ```

  plus an `<Error Condition="!Exists('...StyleBro.Analyzers.targets')" ... />` line in the project's
  `EnsureNuGetPackageBuildImports` target, as NuGet adds for every package with build targets. Don't use a
  `PackageReference` there: it makes NuGet restore the project the PackageReference way, its `packages.config` packages
  are no longer restored, and a fresh clone doesn't build. A `PackageReference` in a `Directory.Build.props` that also
  serves SDK-style projects needs a condition that keeps it away from them:

  ```xml
  <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-alpha.1" PrivateAssets="all" Condition="'$(UsingMicrosoftNETSdk)' == 'true' or !Exists('$(MSBuildProjectDirectory)\packages.config')" />
  ```

  `bro` names the packages.config projects and says this in its report.
- **`--diff` leaves `packages.config` projects out**: the copy it formats has no git-ignored `packages` folder, and
  `dotnet restore` can't restore `packages.config`. It previews the other projects and names the ones it left out; only
  when every project uses `packages.config` does it stop. Other old-style projects preview like `format` runs:
  unrestored, so the preview has no StyleBro changes for them, and it says which. To see the changes in the
  `packages.config` projects, on a clean working tree: run the command without `--diff`, restore with MSBuild as above,
  run `bro format`, and review `git diff` (`git checkout .` undoes it).
- **Next to multi-targeted projects**, the old-style ones get a `dotnet format` run of their own (`== old-style
  projects`): they have no target framework for the per-framework runs. When `dotnet format` can't load them there, it
  says so, and `--verify-no-changes` says `Not verified` instead of clean.
- If StyleCop came in through `packages.config` or an `<Analyzer Include="..\packages\StyleCop.Analyzers.1.0.2\...">`
  hint path, `bro migrate` reads its version there: rules that version doesn't have stay off.
