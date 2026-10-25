# Trial 54: Finbuckle/Finbuckle.MultiTenant (random, seed 54)

main 3765bade, 173 .cs, net10.0 only, `src/Directory.Build.props` (TreatWarningsAsErrors, AnalysisLevel latest-Recommended,
packages.lock.json per project) and none for test/ and samples/, no .editorconfig, no analyzers. Path A (init).
StyleBro main dc107c4, 0.8.0-trial54.1.

Setup (getting-started step 1): the reference in `src/Directory.Build.props` and in the 4 test and 2 sample csproj files
(no props reaches them).

## Results (default engine)

| Step | Exit | Time |
|---|---|---|
| base: build, TWAE Debug/Release, tests | 0 | 607/607 pass (258 + 157 + 105 + 87) |
| `bro preview init` | 0 | 113 s: 84 files, 189 changes, clean after 1 run |
| init --write | 0 | root Directory.Build.props created for the preset (test/ and samples/), src/'s gets it too |
| `bro format` | 0 | 96 s, 84 files, run 2 clean |
| `bro check` | 0 | cached (nothing changed since format's clean run) |
| `bro check --build` | 0 | Debug and Release: no findings, no new errors |
| TWAE Debug/Release | 0 | |
| tests Debug/Release | 0 | 607/607 |

The preview put the reference in a new root Directory.Build.props (for test/ and samples/) and in src/'s; the same 84 files
as the real run. The lock files changed only by the added package. No BOM or final-newline changes (no .editorconfig; 23
files with a BOM, 68 without a final newline, kept).

## Findings

1. **init's summary counted BRO1306 twice**: "Turned 13 rules off because your code mixes both forms: ... BRO1306
   (non-private fields), BRO1306 (private constants/static readonly fields)": 12 rules. Two conventions turn the same rule
   off (`FieldPascalCase` for non-private fields and for private constants). Fixed: the summary has one entry per rule
   (`BRO1306 (non-private fields; private constants/static readonly fields)`) and counts rules
   (`Conventions.ByRule`, `Summary_CountsARuleTwoConventionsTurnOffOnce`). Cosmetic: the .editorconfig was right.
