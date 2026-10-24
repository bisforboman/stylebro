# Trial 51: sebastienros/esprima-dotnet

- Repo: https://github.com/sebastienros/esprima-dotnet at `8ba15fcf9a8358c2fc7b5db8bd5e76f93a0e667c` (random pick 3 of 3,
  seed 51, index 47).
- StyleBro: main at `73c00c5` as `0.8.0-trial51`. Environment: `NUGET_CERT_REVOCATION_MODE=offline`, `NuGetAudit=false`,
  `DOTNET_ROLL_FORWARD=Major`, `CI=true`. No global.json, no nuget.config.
- Repo setup: `Esprima.sln`, 7 projects: Esprima (net462;netstandard2.0;netstandard2.1;net8.0, `AnalysisLevel`
  latest-Recommended), Esprima.SourceGenerators (in-solution source generator, netstandard2.0), Esprima.Tests
  (net462;net8.0, xUnit 2 + Verify), Esprima.Tests.SourceGenerators, Esprima.Tests.Test262 (generates its tests from the
  test262 suite before the build), two samples. TreatWarningsAsErrors in Directory.Build.props, central package management
  (Directory.Packages.props, GlobalPackageReference for SourceLink), a 312-line .editorconfig with many severities,
  `UseArtifactsOutput`. `test/Esprima.Tests/Fixtures.RegExp/Generator/` has its own Directory.Build.props and project (not in
  the solution; it links `test/Esprima.Tests/Helpers/JavaScriptStringHelper.cs`). `* text=auto`, and two files committed
  with CRLF. 249 .cs.
- Left out: Esprima.Tests.Test262's `dotnet test262 generate` (downloads the suite) exits 255 here, so that project fails
  the same way before and after (MSB3073) and its tests don't run; net462 tests need Mono.

## Commands

`.trials/trial51/run.sh esprima-dotnet <clone> <tag> [--engine host]` with `env.sh`:

```
bro preview init [--engine host]
# step 1 with central package management: <PackageVersion Include="StyleBro.Analyzers" Version="0.8.0-trial51" /> in
# Directory.Packages.props, <PackageReference Include="StyleBro.Analyzers" PrivateAssets="all" /> in Directory.Build.props
# (not in the Generator's Directory.Build.props, which the preview also edits: that project isn't in the solution)
bro init --write
bro format [--engine host]
bro check [--engine host]          (+ --json)
bro check --build [--engine host]  (+ --json)
dotnet build Esprima.sln -c Debug|Release --no-incremental -p:TreatWarningsAsErrors=true
dotnet test test/Esprima.Tests -c Debug|Release -f net8.0; dotnet test test/Esprima.Tests.SourceGenerators -c Debug|Release
```

Extra: the Generator project built with TWAE before and after; the preview patch applied to a fresh clone (`pv`), compared
with `default` and its tests run.

## Baseline (untouched code)

| Build / test | Result |
|---|---|
| Debug, Debug / Release TWAE | fail only on Esprima.Tests.Test262 (MSB3073, `dotnet test262 generate` exit 255) |
| Generator (outside the solution), TWAE | green |
| Tests Debug / Release | Esprima.Tests 7,880/7,880, Esprima.Tests.SourceGenerators 2/2 |

## Results

| Command | Default: exit | time | Host: exit | time | Verdict (both) |
|---|---|---|---|---|---|
| `bro preview init` | 3 | 749 s | 3 | 305 s | 119 files, 3,569 changes; 7 kept findings (BRO1303 renames to `_camelCase`, names in strings / code no run loads), baselined; Not checked: Esprima in Release (in-solution generator: only a build checks it) |
| init | 0 | 22 s | 0 | 17 s | 76 lines in .editorconfig; 9 rules off (mixed), 2 off (not followed) |
| `bro format` | 3 | 1176 s | 3 | 472 s | run 1 changed 119 files, run 2 none; "Not verified": Esprima in Release (honest, by design: `bro check` builds it) |
| `bro check` | 0 | 320 s | 0 | 120 s | builds Esprima in Release ("no findings"), Clean with 7 kept findings |
| `bro check --build` | 3 | 776 s | 3 | 256 s | "Not verified": Test262 didn't compile, as in the untouched build (honest) |
| TWAE Debug / Release | 1 / 1 | | 1 / 1 | | only MSB3073 (Test262), as untouched |
| Generator TWAE (after) | 0 | | | | green (`JavaScriptStringHelper.cs`, which the solution's format changed, still compiles there) |
| Tests Debug / Release | 0 / 0 | | 0 / 0 | | 7,880 + 2 each |

Timings with other repositories' jobs running; the default engine's ran under heavier load.

### Engines compared

Identical: trees (`diff -rq` empty), exit codes, verdicts, logs of preview, format, check and check --build (apart from
timings and the order of the `Checked` lines), `--json` of check and check --build. No PR #287 load note here.

## Finding 1: the preview turns files committed with CRLF into LF (and breaks a test), where init and format keep them

Preview vs format: the patch applied to a fresh clone differs from the real run in `.editorconfig` and
`test/Esprima.Tests/AstToJavascriptTests.cs`:

```diff
# real run's .editorconfig (the preview's lacks the section)
-# init: C# files' line endings: LF in 234 of 234 files in your code
+# init: C# files' line endings: LF in 234 of 236 files in your code
+[test/Esprima.Tests/AstToJavascriptTests.cs]
+# init: a string literal in it spans lines, and another ending would change its value
+end_of_line = crlf
```

The patch has no CR at all: applied, it makes AstToJavascriptTests.cs (and src/Esprima/Utils/AstToJavaScriptConverter.cs)
LF throughout, and `ToJavaScriptTest2` then fails (`Expected: "...\\\r\n   corresponding lo..."`, `Actual: "...\\\n..."`):
7,879/7,880. The real init and format keep the file CRLF and all tests pass.

Cause: both files are committed with CRLF (`git ls-files --eol`: `i/crlf w/crlf attr/text=auto`). The preview copies the
working tree and makes the copy a fresh repository (`PreviewCommand.MakeRepository`); `git add` there normalizes them
under `* text=auto`, so the copy's index holds LF while its working tree holds CRLF. Init (`Conventions.ConvertedByGit`)
then counts them as "converted by git" (left out: 234 of 234, no crlf section), and the patch is computed from the LF
index.

Smallest reproduction: a repository with `Crlf.cs` (CRLF, a multi-line verbatim string) committed before
`.gitattributes` (`* text=auto`) and 10 LF files; the preview's patch has no `[Crlf.cs]` section.

Fix (`25b8e26`): `MakeRepository(copy, ..., source)` writes `/<path> -text` into the copy's `.git/info/attributes` for the
files the source's index holds with CRLF or mixed endings (`PreviewCommand.CommittedWithCrLf`, from `git ls-files --eol
-z`; paths with whitespace, quotes or glob characters left out). Test: `MigrationTests.Preview_TheCopy_KeepsFilesCommittedWithCrLf`
(the copy's index, and a preview with a stand-in format whose patch must have the `[Crlf.cs]` section). Mutation entries
`Migrate/PreviewCommand-ba287ae5.psd1` (call site), `-98548a69` (the attributes write), `-dc7da6dd` (the parser).

## Notes (no finding)

- The preview adds the reference to `test/Esprima.Tests/Fixtures.RegExp/Generator/Directory.Build.props` too (it doesn't
  import the root one; getting-started says to), although that project isn't in the solution and format never formats its
  Program.cs: built after the preview's patch, it reports 4 BRO1508 warnings (not errors: its props sets no TWAE). My step 1
  left it out, so `default` and `pv` differ there (and in the order of PackageVersion lines in Directory.Packages.props).
- BRO1303 kept 7 renames with reasons, baselined; check after format: Clean with those 7 listed.

## Final re-check (StyleBro at `8768a53`'s code, `0.8.0-trial51.2`)

Fresh clones after the container restart, both engines: preview 3 (Esprima in Release, by design), format 3 (the same),
check 0 (Clean, 7 kept), check --build 3 (Test262, honest), TWAE Debug/Release only MSB3073 (Test262) as untouched, tests
7,880 + 2 x 2. Trees, exit codes, verdicts, logs and --json identical on both engines.

Finding 1 is fixed: the preview counts "234 of 236 LF", writes the `[test/Esprima.Tests/AstToJavascriptTests.cs]
end_of_line = crlf` section and keeps the file CRLF (`i/crlf` after `git apply`); the patch on a fresh clone gives exactly
the formatted tree in every .cs file and .editorconfig (only the three props files step 1 edited by hand differ, as before).
