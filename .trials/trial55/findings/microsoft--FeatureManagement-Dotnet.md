# microsoft/FeatureManagement-Dotnet (trial 55, random: entry 62, draw 19)

main 787f8199, 192 .cs: Microsoft.FeatureManagement.sln, libraries netstandard2.0/2.1 (+ net8.0/net10.0), tests
net8.0;net10.0 and one net48;net8.0;net10.0 (net48 is in no library: the nearest framework is netstandard2.0), examples,
EnforceCodeStyleInBuild, `.editorconfig` with file headers and `insert_final_newline = true`, NuGet.Config `<clear />`, no
analyzers (init).

## Baseline (untouched)

Build green. TWAE Debug/Release fail at restore: NU1510 (packages that won't be pruned) as errors in the two test
projects; with `-p:WarningsNotAsErrors=NU1510` they compile and fail on 24 ASPDEPR008 (obsolete WebHost/TestServer APIs
under net10.0). Tests: 87 + 21 + 39 per framework pass; `dotnet test` exits 1 (net48 can't run on Linux).

## Default engine (host)

- preview init 93 s: 46 files, 156 changes, clean after 1 run. init: 6 rules off for mixed code, 3 off as not followed
  (BRO1404 52 of 65 types without a modifier, BRO1604, BRO1149), BRO1120 off for the `//` file headers, and trial 53's
  final-newline note ("1 of 192 C# files ends without a line break"): exactly that file
  (tests/Tests.FeatureManagement/VariantServices.cs) got one, nothing else changed in bytes (bytes.py).
- format 92 s, clean on run 2, exit 0; the load-warning note names the restore warning (NU1510), as #287 meant.
- check 55 s exit 0; check --build 119 s exit 3: "Formatting is clean. But: Not verified: the Debug, ... builds failed,
  as the untouched code's do (above)" with the 4 NU1510 errors per configuration listed as the untouched code's: honest.
- TWAE (NU1510 left a warning): the same 24 ASPDEPR008 errors as the untouched code, Debug and Release, same lines; tests
  identical to the baseline.

## Soak

Both engines identical: exit codes, trees, --json (check, check --build), every log (check --build's NU1510 lines come in
build order), tests and TWAE errors. Compare-Format.ps1: "Same". Times (s, host/dotnet-format): preview 93/160, format
92/230, check 55/79, check --build 119/129; Compare-Format check 80/248, format 73/142, recheck 50/53.
No findings.
