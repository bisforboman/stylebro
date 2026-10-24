# autofac/Autofac.Extensions.DependencyInjection (trial 53, random entry 57, draw 13)

Commit 206de12cc40baa58cc6f807e005f99d2e44c1ae4 (develop). StyleBro main 241bca8, packed as 0.8.0-trial53.1.
Setup: path B, `bro migrate . --write`, then StyleCop.Analyzers 1.2.0-beta.556 replaced by StyleBro.Analyzers in the six
csproj files (the reference is per project). Local workaround: global.json 10.0.203 lowered to 10.0.100 (latestFeature).

## Baseline (untouched)

Debug build green; TWAE Debug/Release green. Tests, test/ projects only (the Bench project's "tests" run real
BenchmarkDotNet benchmarks in Release, minutes each, so `dotnet test` on the solution was cut there and left out):
Test 571 (net10.0) + 559 (net8.0), Integration.Test 1 + 1, in Debug and Release.

## Both engines

Migrate read build/Test.ruleset, build/Source.ruleset, build/stylecop.json and Sonar's defaults (136 of 165 StyleCop rules
enforced after migrating), wrote 194 lines to .editorconfig and `<StyleBroPreset>none</StyleBroPreset>`; it warned that the
projects weren't restored (fresh clone, honest: a package can bring a global config). preview 0 (0 files, nothing to
change), format 0 (clean on run 1, 4 frameworks), check 0 Clean, check --build 0 Clean (the solution's 8 configurations:
Debug/Release x Any CPU, ARM, x64, x86; 408 s default, 541 s host); TWAE Debug/Release green; tests as the baseline.
IDE0049 held back and said so (SDK 10.0.1xx). The repository's `end_of_line = crlf` is for `*.sln` only (C# files: 72
of 72 LF, written as end_of_line = lf): not the line-ending lens.

Host vs default: trees, exit codes and both --json identical; logs differ only in the order of the per-framework runs.

No findings.
