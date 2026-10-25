# JamesNK/Newtonsoft.Json.Schema (trial 55, random: entry 61, draw 25)

master 5456c4bf, 273 .cs; the only solution is `Src/Newtonsoft.Json.Schema.slnx` (no solution at the root); central package
management (`Src/Directory.Packages.props`), `Src/NuGet.Config` with `<clear />`, `Src/global.json` 10.0.401 (lowered), a
`Src/Newtonsoft.Json.Schema.Tests/Resources/Schemas/global.json` test resource (not an SDK pin: left alone), net35, net40,
net45, net46, netstandard2.0/2.1, net8.0, net10.0 (reference assemblies on Linux), NetAnalyzers, a CodeAnalysis ruleset, no
StyleCop (init). Step 1 in the CPM form (PackageVersion in Src/Directory.Packages.props, PackageReference in Src/Directory.Build.props).

## Baseline (untouched)

Build, TWAE Debug/Release green (SourceLink warns that the trial clone's `origin` is a local path: a trial artifact). Tests:
22 of 797 (net10.0) / 795 (net8.0) fail in Debug and Release, file-system errors on Linux (as trial 54's smoke run).

## Default engine (host)

- preview init 169 s: 136 files, 686 changes, "clean after 1 run", 1 kept finding (BRO1306 `_schemaDiscovery`: a type
  `SchemaDiscovery` is used in the class), baseline "(in the patch): **Src/stylebro.baseline**".
- init: 7 rules off for mixed code, 4 off as not followed (BRO1601, BRO1113, BRO1149, BRO1618); honest counts.
- format 203 s: run 1 changed 136 files, run 2 clean, exit 0; "Baseline: **stylebro.baseline**" (at the root).
  **Finding 1 (fixed):** the preview and format put a new baseline in different places when the only solution is one
  folder down; `bro format Src/Newtonsoft.Json.Schema.slnx` writes Src/ too. Trial 28 (Jackett) decided on the solution's
  folder (`Baseline_GoesNextToTheSolutionsProjects_WhicheverWayFormatRuns`), but `Repeat` passed the folder its paths are
  shown from as the new baseline's folder, so a plain run on the root got the root. Repro `/home/user/spot55/baseline`
  (Src/Lib.slnx, one kept BRO1306): format -> stylebro.baseline, preview and `format Src/Lib.slnx` -> Src/stylebro.baseline;
  both engines alike. Fixed: the run lets `NewBaselineFolder` decide (`--files` too, from the repository root). The root
  file works for the build (GetPathOfFileAbove), so nothing broke; the patch the preview showed just wasn't what format did.
- check 128 s, exit 0, "Clean: 1 finding kept on purpose"; check --build 513 s, exit 0, six configurations (Debug/Release x
  AnyCPU/x64/x86) "no findings, no new errors".
- The load-warning note ("'dotnet format' had warnings loading the workspace in its net10.0 run ... add '--verbosity
  diagnostic'") appears in format, check and check --build. With `bro check --verbosity diagnostic` the host lists it:
  "Msbuild failed when processing the file '.../Newtonsoft.Json.Schema.csproj' with message: The URL of repository remote
  'origin' is invalid" (SourceLink on the trial clone's local origin). So #303's Serilog follow-up (the note says diagnostic,
  the host lists nothing there) does not reproduce here: this warning is listed.
- TWAE Debug/Release green; tests: the same 22 failures (by name) as the baseline, Debug and Release.
- `Not checked: IDE0049 ...` (held back: SDK 10.0.113), a note, verdict unaffected.
