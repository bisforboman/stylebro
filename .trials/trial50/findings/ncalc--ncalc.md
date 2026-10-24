# Trial 50 runner: ncalc/ncalc

- Repository: https://github.com/ncalc/ncalc at `9e875d92aaf7110d66374a98a628662698d55908` (master, "Use Parlot source generator (#533)").
- StyleBro: `claude/trial-50` at `2438d764` packed as `0.8.0-trial50` (StyleBro.Analyzers + StyleBro.Tool) into a local feed
  (`/home/user/feed50`, added as a global NuGet source); `dotnet tool install -g StyleBro.Tool --version 0.8.0-trial50`.
- SDKs: 10.0.113 and 8.0.132 (Ubuntu packages), pwsh: both were preinstalled. `ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`.
- Repo setup: none of StyleCop/StyleSharp/Sonar/CSharpier; Roslynator analyzers; `TreatWarningsAsErrors=true` in
  Directory.Build.props (tests turn it off); libraries target `net462;netstandard2.0;net8.0;net10.0`, NCalc.Parser
  `netstandard2.0;net8.0;net10.0` with the Parlot source generator; NCalc.Benchmarks (net8.0) compiles two of
  NCalc.Parser's files (`<Compile Include="../../src/NCalc.Parser/LogicalExpressionParser.parlot.cs" Link=... />`).
  So `bro init` (not migrate).
- Environment: full clone (Nerdbank.GitVersioning); global.json 10.0.101 lowered to 10.0.100 and committed in the clone
  (noreply identity); `NUGET_CERT_REVOCATION_MODE=offline`, `NuGetAudit=false` (env var), `CI=true`,
  `DiffEngine_Disabled=true`. Clones: `.trials/trial50/ncalc/{base,default,host}` (default and host are local clones of
  base, branch `trial`).

## Targeted check (trial 49's ReSharper fix): holds

`// ReSharper disable once InconsistentNaming` above `public static object IEEERemainder(...)` in
`src/NCalc.Core/Helpers/MathHelper.cs`: after the default engine's format the name and the comment are unchanged (the
method moved from line 266 to 263 only because an `if` merge above it removed three lines). No BRO1300-series finding on
it in check or the builds.

## Commands

```bash
# baseline (base clone)
dotnet build NCalc.slnx -c Debug -p:NuGetAudit=false
dotnet build NCalc.slnx -c Release -p:NuGetAudit=false
dotnet test --solution NCalc.slnx -p:NuGetAudit=false
dotnet test --solution NCalc.slnx -c Release -p:NuGetAudit=false

# default engine (default clone)
bro preview init
bro init --write
cp <preview copy>/Directory.Build.props <preview copy>/Directory.Packages.props .   # the package reference the preview adds
git commit -am "init + StyleBro.Analyzers"
bro format
git commit -am "format run 1"
bro check
bro check --build
dotnet build NCalc.slnx -c Debug; dotnet build NCalc.slnx -c Release
dotnet test --solution NCalc.slnx; dotnet test --solution NCalc.slnx -c Release
bro format                # second run
bro check --json

# host engine (host clone, same steps)
bro preview init --engine host
bro init --write; cp ... ; git commit
bro format --engine host
bro check --engine host
bro check --engine host --json
bro check --build --engine host
dotnet build NCalc.slnx -c Debug; dotnet build NCalc.slnx -c Release; dotnet test --solution NCalc.slnx
```

## Results

### Default engine

| Command | Verdict | Exit | Time |
|---|---|---|---|
| `bro preview init` | "The preview failed: 'bro format' failed (exit code 1)" (NCalc.Benchmarks net8.0 run crashed) | 1 | 146 s |
| `bro init --write` | wrote 76 lines .editorconfig, `<StyleBroPreset>1.0</StyleBroPreset>` | 0 | 7 s |
| `bro format` | "Run 1 failed after changing 59 files" (same crash) | 1 | 162 s |
| `bro check` | "Not verified: 4 parts of the build weren't checked ... the rest is clean" (4 net462 lines; **no line for the crashed NCalc.Benchmarks run**) | 3 | 97 s |
| `bro check --build` | "Not clean: the builds report 96 findings" (32 per configuration, all in `LogicalExpressionParser.parlot.cs` + 1 in `SimpleEvaluationBenchmark.cs`) | 2 | 183 s |
| `bro format` (2nd) | changed nothing, crashed again | 1 | 88 s |
| `bro check --json` | `clean: false`, `exitCode: 3`, `notChecked` = IDE0049 + 4 net462 (no Benchmarks) | 3 | 91 s |

### Host engine

| Command | Verdict | Exit | Time |
|---|---|---|---|
| `bro preview init --engine host` | "Unknown option: --engine" (preview doesn't take it; docs/format-engine.md only names format/check) | 1 | 1 s |
| `bro init --write` | output and diff byte-identical to the default clone's | 0 | 7 s |
| `bro format --engine host` | every run (6 of 6) "crashed", 0 files changed, no verdict line | 1 | 50 s |
| `bro check --engine host` | 5 crashes, only the IDE0049 Not checked line, no verdict line | 1 | 30 s |
| `bro check --engine host --json` | `clean: false`, `exitCode: 1`, `notChecked` = IDE0049 only | 1 | 29 s |
| `bro check --build --engine host` | "Not clean: the builds report 84 findings" (28 per configuration: the build stops at NCalc.Domain) | 1 | 54 s |

Trees after format: **not identical**. Default changed 59 files (311+/199-); host changed none. The difference is not
PR #287's load note: it is finding 2.

### Builds and tests

| | Debug TWAE | Release TWAE | Tests Debug | Tests Release |
|---|---|---|---|---|
| Untouched (base) | 0 errors, 21 warnings (test project, TWAE off there: CS8632, CS0168, CS8605, TUnit) | 0 / 21 | 901/901 | 901/901 |
| Default engine after format | **28 errors**, 25 warnings | **28 errors**, 25 warnings | 901/901 | 901/901 |
| Host engine after format (= untouched + init) | 28 errors (NCalc.Domain, build stops there) | 28 errors | doesn't build | - |

Default engine's 28 errors, all in code only the crashed NCalc.Benchmarks run would have formatted:
21 BRO1523 + 5 BRO1110 + 1 BRO1504 in `src/NCalc.Parser/LogicalExpressionParser.parlot.cs` (Benchmarks compiles it; in
NCalc.Parser it isn't compiled, see below) and 1 BRO1125 in `test/NCalc.Benchmarks/SimpleEvaluationBenchmark.cs`. The 4 new
warnings are BRO1306 kept findings in NCalc.Tests (`x`/`y` lambda parameters in LambdaTests.cs, kept: "the name is in a
string"; TWAE is off in tests). No test that passed before fails after.

## Findings (3)

### 1. `dotnet format` crashes loading NCalc.Benchmarks: format can never finish (default engine)

**What happened.** The net8.0 "part 2 of 2" run (NCalc.Benchmarks alone, through a `.stylebro-format-N.slnf`) crashes in
every `bro format`, `bro check` and `bro preview`:

```
Unhandled exception: System.InvalidOperationException: The solution does not contain the specified document.
   at Microsoft.CodeAnalysis.Shared.Extensions.ISolutionExtensions.GetRequiredDocument(Solution solution, DocumentId documentId)
   at Microsoft.CodeAnalysis.Workspace.<SetCurrentSolutionAsync>g__UpdateAddedDocumentToExistingContentsInSolution|29_1(...)
   at Microsoft.CodeAnalysis.Workspace.<SetCurrentSolutionAsync>g__UnifyLinkedDocumentContents|29_0(...)
   ...
   at Microsoft.CodeAnalysis.Workspace.OnSolutionAdded(SolutionInfo solutionInfo)
   at Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.OpenSolutionAsync(...)
```

`bro format` exits 1 on every run, the preview fails, Benchmarks' view of the code is never fixed, and the TWAE build
keeps 28 errors. A new user has no way to get green with StyleBro alone. bro's note blames
https://github.com/dotnet/roslyn/issues/86001 ("Changes must be within bounds"), which is a different crash.

**Why.** Parlot.SourceGenerator's build targets (`build/Parlot.SourceGenerator.targets`) turn every `**/*.parlot.cs` into
an **AdditionalFiles** item and remove it from Compile. So in NCalc.Parser, `LogicalExpressionParser.parlot.cs` is an
additional document, while NCalc.Benchmarks compiles the same path as a regular document. When MSBuildWorkspace opens a
*solution* (sln, slnx or slnf) containing both, `UnifyLinkedDocumentContents` looks up the related documents by file
path, finds the additional document's id and calls `GetRequiredDocument`, which only knows regular documents: crash.
Opening a *project* (`OpenProjectAsync`) doesn't go through that path and works, even when the project references the
other one.

Bisect on a scratch copy of the untouched repo (plain `dotnet format style NCalc.slnx --verify-no-changes --no-restore`,
no StyleBro): crashes as is; still crashes without `EmitCompilerGeneratedFiles`; still crashes without the Helpers link;
**works without the parlot.cs link**. Also plain, untouched: `.slnf` with just Benchmarks crashes, `.slnf` with Benchmarks
+ Parser + the whole reference closure crashes, `NCalc.slnx` crashes, but `dotnet format style
test/NCalc.Benchmarks/NCalc.Benchmarks.csproj` works (exit 0). Logs: `logs/repro-*.log`.

**Smallest reproduction** (`.trials/trial50/spot/linked/`, SDK 10.0.113; no StyleBro, no generator needed):

```xml
<!-- Lib/Lib.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
  <ItemGroup>
    <Compile Remove="Shared.cs" />
    <AdditionalFiles Include="Shared.cs" />
  </ItemGroup>
</Project>
<!-- App/App.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
  <ItemGroup><Compile Include="../Lib/Shared.cs" /></ItemGroup>
</Project>
<!-- Repro.slnx -->
<Solution><Project Path="App/App.csproj" /><Project Path="Lib/Lib.csproj" /></Solution>
```

```csharp
// Lib/Shared.cs
namespace Lib;

public static class Shared
{
    public static int One() => 1;
}
```

`dotnet format whitespace Repro.slnx --verify-no-changes --no-restore` -> exit 1, the exception above.
`dotnet format whitespace App/App.csproj --verify-no-changes --no-restore` -> exit 0 (also with a ProjectReference to Lib
added; the slnx with that reference still crashes).

**Possible directions for the fixer** (not tried in bro): when a run's group is one project, hand `dotnet format` the
project file instead of a one-project `.slnf`; or detect a file that is AdditionalFiles in one project and Compile in
another and say so instead of pointing at roslyn#86001. Side note: the "Shared files" line names the groups
"NCalc.Core | NCalc.Benchmarks", but the file Benchmarks shares belongs to NCalc.Parser (NCalc.Core only references it).

### 2. `--engine host` crashes in every run and changes nothing

**What happened.** `bro format --engine host` / `bro check --engine host` open the whole solution once
(`StyleBro.Host.Session.OpenAsync`, `src/StyleBro.FormatHost/Session.cs:207`, `OpenSolutionAsync`), which hits finding 1's
crash, so **every** run fails (6 of 6 for format, 5 of 5 for check), including net10.0/net462/netstandard2.0, where
NCalc.Benchmarks isn't even in the run. 0 files changed; exit 1; each run prints "'dotnet format' crashed in the run for
<that run's projects>", blaming 11-12 projects per run and roslyn#86001. The default engine at least formats 59 files.
So the engines' trees differ completely (59 files vs 0), and their exit codes differ for check (3 vs 1) and check --build
(2 vs 1).

**Why.** Same root cause as 1; the host loads one workspace per configuration for all runs, so the one load failure takes
down every run instead of just the Benchmarks group. Stack: `logs/host-format.log` lines 14-28.

**Reproduction.** As in 1: `bro format --engine host` in a git repo with the Repro.slnx above.

### 3. `bro check` (default engine) says "Not verified ... the rest is clean" though a run crashed and isn't named

**What happened.** `bro check` prints the crash, then:

```
Not checked: IDE0049: ... (a note ...)
Not checked: NCalc.Core (net462): 'dotnet format' couldn't load it ...
Not checked: NCalc.DependencyInjection (net462): ...
Not checked: NCalc.LambdaCompilation (net462): ...
Not checked: NCalc.MemoryCache (net462): ...
Not verified: 4 parts of the build weren't checked (the 'Not checked:' lines above); the rest is clean.
```

exit 3; `--json` the same (`exitCode: 3`, `notChecked` without Benchmarks). NCalc.Benchmarks (net8.0) wasn't checked at
all, it isn't among the Not checked lines, and "the rest is clean" is false: `bro check --build` on the same tree finds 32
findings per configuration exactly there (parlot.cs, SimpleEvaluationBenchmark.cs). On the same crash `bro format` exits 1,
the preview exits 1 and `bro check --engine host` exits 1, so default `check` is the odd one out. A false "Not verified":
it should be exit 1 (dotnet format failed), or at least a Not checked line for NCalc.Benchmarks (net8.0).

**Reproduction.** Not reduced. The tiny Repro doesn't show it: there `bro check` exits 1 (correct), both with the crash
as the only run and with a second, succeeding netstandard2.0 run that has a BRO1505 finding
(`.trials/trial50/spot/linked-check/`, `check.log`, `check2.log`). What ncalc adds: the only findings the other runs
report are the 4 kept BRO1306 ones, plus net462 gap lines.

**Why (reading `FormatCommand.Verify`, src/StyleBro.Migrate/FormatCommand.cs ~3540-3805, not confirmed by a debugger).**
The runs' combined code comes back as 2 (NotClean: the kept BRO1306 findings make `--verify-no-changes` say "would
change"), not 1, so the crashed run's failure is gone at that point. Verify then takes the NotClean path; since every
finding line is a recorded kept finding (`LoadKept` from the earlier `bro format`), it skips formatting a copy
(`copied = (0, recorded, [])`), finds `unfixed == 0` and the net462 gaps, and returns NotVerified (3). Nothing on that path
looks at the crash line. Likely needed: a crashed run (the "'dotnet format' crashed in the run for" case) keeps exit 1
whatever the other runs return, or becomes a Not checked gap for its projects.

## Not findings, noted

- net462: `dotnet format` "couldn't load" NCalc.Core/DependencyInjection/LambdaCompilation/MemoryCache for net462 although
  `dotnet build` builds net462 fine here; bro reports it honestly as Not checked lines (known from earlier trials).
- `bro check --build --engine host` counted 28 findings per configuration because the build stops at NCalc.Domain; the
  verdict (Not clean) is right.
- `bro preview` has no `--engine` option (docs only name format/check).
- Format converged on the default engine apart from the crash: the second `bro format` changed nothing.

Logs: `.trials/trial50/ncalc/logs/` (committed). Repro: `.trials/trial50/findings/ncalc-repro/linked/` (copy of `spot/linked/`, which has its own .git and isn't committed); the check logs of the multi-run variant: `findings/ncalc-repro/check*.log`.

## Re-check (fixer's PR #294, `claude/trial-50` at 4c60b3c)

Packed as `0.8.0-trial50r2` (feed `/home/user/feed50r2`, tool updated with `dotnet tool update -g`). Fresh local clones of
`base` (untouched code + the global.json commit): `r2-default`, `r2-host`. Same environment as above. Script:
`.trials/trial50/ncalc/r1-run.sh <clone> [--engine host]`: `bro preview init`, `bro init --write`, StyleBro.Analyzers
0.8.0-trial50r2 in Directory.Build.props/Directory.Packages.props (as the preview adds it), commit, `bro format`, commit,
`bro format` again, `bro check`, `bro check --json`, `bro check --build`, `bro check --build --json`, TWAE
`dotnet build -c Debug/Release`, `dotnet test --solution NCalc.slnx` (Debug, Release). Logs: `logs/r2-default/`,
`logs/r2-host/`.

First try at 990c775 (`0.8.0-trial50r1`, logs `logs/r1-990c775-*`, stopped when 4c60b3c came): fix 3 already held
(check exit 1 on the crash instead of 3). Fix 1 didn't fire: `AdditionalFilesOf` evaluated multi-targeted NCalc.Parser
without a TargetFramework, and Parlot's targets add the AdditionalFiles item only in the per-framework evaluations
(`-getItem:AdditionalFiles` was [] in the outer evaluation, ['LogicalExpressionParser.parlot.cs'] with
`-p:TargetFramework=net8.0`). The fixer fixed that in 4c60b3c.

| Command | Default: exit, time | Host: exit, time |
|---|---|---|
| `bro preview init [--engine host]` | 3 (Clean apart from Not checked), 268 s | 3, 266 s (no "Engine:" line in the preview) |
| `bro init --write` | 0, 7 s | 0, 6 s |
| `bro format` | 3: run 1 changed 61 files, run 2 changed 0, "Not verified: 4 parts" | 3, same, 285 s |
| `bro format` (again) | 3: 0 files changed, 83 s | 3, 85 s |
| `bro check` | 3, 46 s | 3, 57 s |
| `bro check --json` | `clean: false`, `exitCode: 3`, 47 s | the same, 57 s |
| `bro check --build` | 3: Debug/Release/SignedRelease "no findings, no new errors (21 errors the untouched code has too)", 214 s | 3, same, 220 s |
| `bro check --build --json` | `exitCode: 3`, 215 s | the same, 219 s |
| TWAE `dotnet build` Debug / Release | **0 errors**, 21 warnings each (= baseline) | 0 errors, 21 warnings |
| `dotnet test` Debug / Release | 901/901, 901/901 | 901/901, 901/901 |

The Not checked lines are the same everywhere: IDE0049 (a note) + NCalc.Core/DependencyInjection/LambdaCompilation/
MemoryCache (net462, "references didn't load"), in the text and in `--json` `notChecked`.

- **Finding 1: fixed.** No run crashes on either engine (no "Unhandled exception" or "crashed in the run" in any log).
  The "Shared files:" line now says: "Some projects have a file as an additional file (AdditionalFiles) that another
  compiles (src/NCalc.Parser/LogicalExpressionParser.parlot.cs): loaded together, 'dotnet format' crashes ("The solution
  does not contain the specified document"), so they run apart, and a project that loads both itself runs on its project
  file. Others may compile the same files with other '#if' symbols or settings (roslyn#86001)." NCalc.Benchmarks gets
  formatted: `LogicalExpressionParser.parlot.cs` (49+/32-) and `SimpleEvaluationBenchmark.cs` changed, and the 28 TWAE
  errors are gone (0 errors in Debug and Release). Format settles: the second run and a second `bro format` change
  nothing.
- **Finding 2: fixed.** The host prints once per command "Engine: the host couldn't load the workspace (The solution does
  not contain the specified document), so 'dotnet format' runs the checks and fixes." (format, format again, check,
  check --build). The trees after format are **byte-identical** across engines (`diff -r`, excluding .git/obj/bin). Exit
  codes match in every row. `--json` is the same apart from `path`. The text output matches line for line apart from the
  Engine line and progress order, with one exception: in the first `bro format`'s kept list, BRO1306's "first:" reference
  is `DecimalsTests.cs(142)` with the default engine and `(141)` with the host. That's cosmetic: the reference is taken at a
  different point of the run, and the later check logs agree.
- **Finding 3: fixed.** On the crash, check now exits 1 (seen at 990c775). With nothing crashing, "Not verified" (3) is
  true: the 4 net462 projects aren't loaded by `dotnet format`, and the build doesn't prove their IDE rules because the
  repo has no `EnforceCodeStyleInBuild` (FormatCommand.Build.cs:312 only counts the build as proof then).
- **Targeted: holds.** `// ReSharper disable once InconsistentNaming` + `public static object IEEERemainder(...)`
  (MathHelper.cs:262-263) are unchanged.
- No test that passed on the untouched code fails, no false Clean, no false Not verified, no format needing more than one
  changing run. **No new findings.**

Observations (not verdict findings, for the fixer to weigh):

- **More runs than needed.** net10.0 now runs in 11 parts (16 runs in all, 6 before), although no net10.0 project
  compiles parlot.cs (NCalc.Benchmarks is net8.0 only). Format run 1 went from 162 s to ~273 s; check --build from 183 s to
  214 s. The split looks broader than the AdditionalFiles conflict needs (only the net8.0 view has Parser + Benchmarks).
- `bro check --build`'s verdict line still says "build it (bro check --build)" right after that command's builds. It
  would help if it named `EnforceCodeStyleInBuild` as what lets the build prove the net462 parts.
- `bro preview init --engine host` is now accepted, but prints no Engine line, so it's hard to tell which engine the
  preview used.
