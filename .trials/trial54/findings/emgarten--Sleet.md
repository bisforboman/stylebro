# Trial 54: emgarten/Sleet (random, seed 54)

main 833134d1, 228 .cs, net8.0;net9.0;net10.0 libraries + net10.0 tests (MTP), `build/common.props` imported by every csproj
(TreatWarningsAsErrors, EnforceCodeStyleInBuild, AnalysisMode Recommended), central package management, root `.editorconfig`
(`charset = utf-8` followed by all files, `dotnet_sort_system_directives_first`), no StyleCop. global.json 10.0.401 lowered
locally; `NuGet.Config` `<clear/>`s (trial feed added there). Path A (init). StyleBro main dc107c4, 0.8.0-trial54.1.

Setup: `PackageVersion` in Directory.Packages.props, `PackageReference` in build/common.props (where the repo puts shared
references).

## Results (default engine; the host engine identical)

| Step | Exit | Notes |
|---|---|---|
| base: build, TWAE Debug/Release | 0 | |
| base tests Debug/Release | 2 | 1,881 pass, 1 fails (Sleet.CliTool.Tests InstallToolVerifySuccess: installs a tool from nuget.org), 43 skipped |
| `bro preview init` | 0 | 122 files, 421 changes, clean after 1 run; 1 kept finding, baselined |
| init --write | 0 | preset pinned in a new root Directory.Build.props (auto-imported after common.props); the usings-order note (12 of 204 files) |
| `bro format` | 0 | net10.0 run only (net8.0/net9.0 compile the same), 122 files, run 2 clean |
| `bro check` | 0 | Clean, 1 kept finding |
| `bro check --build` | 0 | Debug and Release: no findings, no new errors |
| TWAE Debug/Release | 0 | |
| tests Debug/Release | 2 | the same 1 failure as the untouched code |

Kept: `src/SleetLib/AssemblyVersionHelper.cs(11,40)` BRO1306 `_lock` -> `Lock` ("at a use of the name, the new name would
mean something else": `System.Threading.Lock` on net9+), right. `Not checked:` notes for xunit.analyzers 2.2.0 (no Fix All)
and IDE0049 (SDK 10.0.1xx), verdict clean as documented. No BOM/final-newline changes.

## Findings

None.
