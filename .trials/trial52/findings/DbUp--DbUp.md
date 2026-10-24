# DbUp/DbUp (trial 52, random entry 51)

Commit 41228ce747979fb52e1a59e3eec5823e6de869c8. StyleBro main 8247120 (after #297), packed as 0.8.0-trial52.1.
Setup: path A, step 1 in `src/Directory.Build.props` (the only one; none at the root), then `bro init --write`.

## Baseline (untouched)

Debug build green; TWAE Debug/Release green; tests 100/100 (net8.0) in Debug and Release.

## Finding 1: a committed .nupkg hides src/ from init and preview (both engines)

DbUp commits `src/dbup.1.2.3.nupkg` next to `src/DbUp.sln` and `src/Directory.Build.props`.
`StyleCopSetup.EnumerateFiles` skips every folder holding a `.nupkg` as an extracted NuGet package (trial 31), so the whole
`src/` folder (all projects and code) was invisible to init and the preview:

- `bro preview init` added the package reference to a NEW root `Directory.Build.props` (`Migration.PropsFiles` found no
  project, so it defaulted to the root), which `src/Directory.Build.props` hides; format then failed: "StyleBro's analyzers
  aren't loaded" (exit 1, "The preview failed").
- `bro init --write` wrote `<StyleBroPreset>1.0</StyleBroPreset>` into a new root `Directory.Build.props` nothing imports,
  and judged the conventions from no code at all ("too little code here to tell its conventions ... StyleBro's defaults
  apply"), so the code's own conventions were never read.
- `PreviewCommand.InPackage` has the same test: untracked files under `src/` would stay out of the preview's copy.

Repro: a folder with `App.csproj`, `Own.cs`, `Directory.Build.props` and `old.1.0.0.nupkg`: `Migration.PropsFiles(root)`
returns the root's file. Cause: StyleCopSetup.cs `EnumerateFiles` (`files.Any(f => f.EndsWith(".nupkg"))`).
Fix idea: a folder with a project, solution or Directory.Build.* file isn't an extracted package.

Consequences on both engines (first round, identical trees, exit codes and --json):
- `bro format`: "Run 1: 0 files changed, clean" (the files under src/ weren't in its view either); `bro check`: "Clean"
  (exit 0), a dishonest verdict; `bro check --build`: exit 2, honest ("Not clean: the builds report 2 findings",
  BRO1001 in src/Tests.Common/RecordingDb/RecordingDbCommand.cs).
- TWAE Debug/Release failed (2 x BRO1001 as errors: the repository's own TreatWarningsAsErrors), so `dotnet test`
  didn't build: tests 0/100 against 100/100 untouched.

## After the fix (dev build of 798fed3+fix, fresh clone, same steps, default engine)

`bro init --write` read the code (and src/.editorconfig, where it now writes its whitespace settings), set the preset in
src/Directory.Build.props; preview 0 (42 files, clean after 1 run), format 0 (42 files + settings), check 0 Clean,
check --build 0 Clean (Debug, Release: no findings, no new errors), TWAE Debug/Release green, tests 100/100 x 2.
