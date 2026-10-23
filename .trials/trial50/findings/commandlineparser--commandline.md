# Trial 50 runner: commandlineparser/commandline

- Repo: https://github.com/commandlineparser/commandline at `1e3607b97af6141743edb3c434c06d5b492f6fb3` (random pick 1 of 3, seed 50).
- StyleBro: branch `claude/trial-50` at `26e9eb8`, packed as `0.8.0-trial50` (StyleBro.Analyzers + StyleBro.Tool) into
  `/home/user/feed50`, the feed added as a global NuGet source, `dotnet tool install -g StyleBro.Tool --version 0.8.0-trial50`.
- SDKs: 10.0.113 and 8.0.132 (Ubuntu packages), pwsh 7: all preinstalled; only the `bro` symlink was needed
  (`ln -s ~/.dotnet/tools/bro /usr/local/bin/bro`). No global.json, no nuget.config in the repo.
- Environment: `NUGET_CERT_REVOCATION_MODE=offline`, `NuGetAudit=false` (as an environment variable, so it reaches every
  MSBuild call, bro's included), `DOTNET_ROLL_FORWARD=Major` (tests target netcoreapp3.1).
- Repo setup: library `src/CommandLine` (netstandard2.0; net40; net45; net461, strong-named, net4x through
  Microsoft.NETFramework.ReferenceAssemblies), tests `tests/CommandLine.Tests` (net461; netcoreapp3.1, xUnit 2.4.1,
  Microsoft.NET.Test.Sdk 16.0.1), 182 .cs files, CRLF committed, `.editorconfig` with `end_of_line = crlf`, no
  StyleCop/analyzers, no TreatWarningsAsErrors of its own. net461 tests can't run on Linux (no Mono): only netcoreapp3.1 ran.

## Commands

The script is `.trials/trial50/commandline/run.sh <clone> <tag> [engine args]`; per clone:

```
bro preview init [--engine host]
# getting-started step 1: <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial50" PrivateAssets="all" /> in Directory.Build.props
bro init --write
bro format [--engine host]
bro check [--engine host]
bro check [--engine host] --json
bro check --build [--engine host]
bro check --build [--engine host] --json
dotnet build CommandLine.sln -c Debug|Release --no-incremental -p:TreatWarningsAsErrors=true
dotnet test tests/CommandLine.Tests -c Debug|Release -f netcoreapp3.1
```

Baseline (clone `base`, untouched): `dotnet build CommandLine.sln -c Debug`, the two TWAE builds and the two test runs above.
Extra: `bro preview format [--engine host] --diff=<file>` on a third clone after the package + `init --write` (committed).

## Baseline (untouched code)

| Build / test | Result |
|---|---|
| Debug (no TWAE) | 0 errors, 2 warnings (xUnit2013 at `Unit/Core/GetoptTokenizerTests.cs(93,13)`, net461 + netcoreapp3.1) |
| Debug TWAE | fails: 2 errors, xUnit2013 (same place, both test TFMs) |
| Release TWAE | fails: 2 errors, xUnit2013 |
| Tests Debug / Release (netcoreapp3.1) | 684 / 684 passed |

## Results

| Command | Default: exit | time | Host: exit | time | Verdict (both) |
|---|---|---|---|---|---|
| `bro preview init` | 0 | 87 s | 1 | 0 s | default: 142 files, clean after 1 run; host: `Unknown option: --engine` (finding 2) |
| `bro init --write` | 0 | 4 s | 0 | 4 s | 65 lines in .editorconfig, `StyleBroPreset` 1.0; 12 rules off by conventions |
| `bro format` | 0 | 90 s | 0 | 48 s | run 1: 142 files changed; run 2: 0 changed, clean |
| `bro check` | 0 | 21 s | 0 | 19 s | clean |
| `bro check --json` | 0 | | 0 | | `clean: true`, `exitCode: 0` |
| `bro check --build` | 2 | 56 s | 2 | 55 s | "Formatting is clean. But: Not clean: the Debug, Release builds failed where the untouched code's didn't": CS0017 (finding 1) |
| `bro check --build --json` | 2 | | 2 | | `clean: false`, `exitCode: 2` |
| Debug TWAE build | 1 | 9 s | 1 | 8 s | 2 errors CS0017 (netcoreapp3.1; the `--no-incremental` build stops before net461) |
| Release TWAE build | 1 | 8 s | 1 | 8 s | 2 errors CS0017 |
| Tests Debug / Release | 1 / 1 | 4 s | 1 / 1 | 4 s | don't build (CS0017) |
| `bro preview format --diff` (third clone) | 0 | 72 s | 1 | 8 s | host: "The preview failed: 'bro format' failed (exit code 1)" (finding 2) |

`Not checked:` on every check/format, both engines, identical: `IDE0049: this SDK's 'dotnet format' ignores the multi-target
guard, so these fixes are held back and not checked (a note: ...; the verdict doesn't count them)`.

### Engines compared

- Trees after format (and after everything): `diff -rq --exclude=.git --exclude=bin --exclude=obj default host` is empty:
  byte-identical.
- Exit codes and verdicts: identical (table above).
- `--json` (check, check --build): identical apart from `path` (the clone's folder); `notChecked` the same one IDE0049 line.
- Logs: differ only in the order of the `Checked <tfm> (run i of 4)` lines and the default engine's
  "'dotnet format' had warnings loading the workspace in its netcoreapp3.1 run" note (PR #287's documented difference).
- Preview vs format (default): the preview patch applied to a fresh clone gives exactly the format tree for every .cs file
  and .editorconfig. Only Directory.Build.props differs: the preview puts StyleBro.Analyzers in its own `<ItemGroup>`, my
  step-1 edit put it into the existing one.

### Verdicts vs builds/tests

- `bro check` "clean" is right: format's rules leave nothing (TWAE builds have no BRO/IDE errors).
- `bro check --build` "Not clean" (exit 2) is right: the TWAE builds fail on CS0017, which the untouched code doesn't have.
  The baseline's xUnit2013 error is gone (format fixed it: xunit assertion rules are in format's set).
- Format converged: one changing run, the second clean. No false Clean, no false Not verified.
- With the one rename of finding 1 undone by hand (clone `default-nomain`): TWAE builds fail only on 4 BRO1309 errors (the
  undone rename, expected), and 684 / 684 tests pass in Debug and Release. So finding 1 is the only regression.

## Finding 1: BRO1309 renames `__Main` to `Main` and creates a second entry point (CS0017; the netcoreapp3.1 tests stop building)

What happened: `tests/CommandLine.Tests/Unit/Issue389Tests.cs` has a nested `Program` class with
`internal static int __Main(string[] args)` (the test calls it directly). BRO1309 (PascalCase names, "Rename '__Main' to
'Main'") renamed it. The test project's netcoreapp3.1 build gets an auto-generated entry point from Microsoft.NET.Test.Sdk
(`GenerateProgramFile`, .NET Core only), so the renamed method is a second entry point: `error CS0017: Program has more
than one entry point defined` (Debug and Release; net461 test builds are libraries and don't fail). All 684 tests that
passed before can't build and run any more. Both engines; the preview shows the same rename without a warning.

```diff
--- a/tests/CommandLine.Tests/Unit/Issue389Tests.cs
+++ b/tests/CommandLine.Tests/Unit/Issue389Tests.cs
@@ -15,7 +14,7 @@
         [Fact]
         public void CallMain_GiveHelpArgument_ExpectSuccess()
         {
-            var result = Program.__Main(new[] { "--help" });
+            var result = Program.Main(new[] { "--help" });
@@ -23,22 +22,21 @@
         internal class Program
         {
-
-
-            internal static int __Main(string[] args)
+            internal static int Main(string[] args)
```

Smallest reproduction (`.trials/trial50/spot/main-rename/`, own git repo; `bro format` exit 0, then `dotnet build`: CS0017):

```xml
<!-- Repro.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="StyleBro.Analyzers" Version="0.8.0-trial50" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

```csharp
namespace Repro
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return Helper.__Main(args);
        }
    }

    internal static class Helper
    {
        internal static int __Main(string[] args)   // BRO1309: Rename '__Main' to 'Main'
        {
            return args.Length;
        }
    }
}
```

```ini
# .editorconfig
root = true

[*.cs]
indent_style = space
```

Why: the renamer's conflict checks look at same-named members in scope, but `Main` is special to the compiler: any static
`Main` with an entry-point signature (`void`/`int`/`Task`/`Task<int>` return, no parameters or `string[]`) in an executable
compilation is an entry-point candidate, wherever it is declared. A rename to `Main` in a project that already has an
entry point (its own, or a generated one: Microsoft.NET.Test.Sdk on .NET Core, top-level statements) adds a second one.
`PascalCaseNamingAnalyzer.NeedsAsyncSuffix` already leaves `Main` alone for BRO1314; BRO1309 has no such guard for a NEW
name of `Main`. Since the generated test entry point only exists in some target frameworks' compilations, a check on the
symbol's own compilation (OutputKind) wouldn't see it in a library-looking net461 run: skipping (or keeping, with a kept
reason) any rename whose new name is `Main` for a static method with an entry-point shape looks like the safe fix.

Logs: `logs/default-checkbuild.log`, `logs/default-twae-Debug.log`, `logs/host-twae-Debug.log`, `logs/default-nomain-*.log`,
`spot/main-rename-format.log`.

## Finding 2: `bro preview` with `--engine host` fails (init: "Unknown option", format: dotnet format's usage)

What happened:
- `bro preview init --engine host` exits 1 at once with `Unknown option: --engine` and the usage text (`Dispatch` in
  Program.cs: `init` and the preview have a fixed option list without `--engine`).
- `bro preview format --engine host --diff=<file>` copies the repository, then "Format run 1... 8s / The preview failed:
  'bro format' failed (exit code 1)" with `dotnet format`'s own usage dump: the preview's inner format passes `--engine host`
  on to `dotnet format`, which doesn't know it. It still prints the IDE0049 `Not checked:` line and "Done in 8s." after the
  failure, and keeps the temp copy. Without `--engine` the same preview passes (exit 0, 72 s).

So the in-process engine can't be previewed, although the usage says `bro preview <init|migrate|format> ... [options]`
and `bro format`/`bro check` take `--engine host`. Not a wrong verdict or a code change, but a host-engine difference that
isn't documented (docs/format-engine.md mentions only `check` and `format`), and the format variant fails with a confusing
message. Repro: any repository with StyleBro set up, `bro preview format --engine host`. Either honour the option in the
preview's format runs, or reject it up front with a clear message for all three preview forms.

Logs: `logs/host-preview.log`, `logs/pvf-host.log`, `logs/pvf-default.log`.

## Not findings (checked)

- Directory.Build.props got one stray CR in the trial clones: my own step-1 `sed` inserted `\r\n` into an LF file, and
  init then matched that CRLF. A clean clone + `bro init --write` writes LF (probed). run.sh now inserts `\n`.
- 65 files' line endings changed (ENDOFLINE) and 19 got a final newline: the repository's own `.editorconfig`
  (`end_of_line = crlf`, `insert_final_newline = true`); the committed files were mixed.
- IDE0049 held back on SDK 10.0.113: documented note, not counted in the verdict.
