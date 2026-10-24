# AArnott/Nerdbank.Streams (trial 53, random entry 56, draw 54)

Commit e1743fd5d41733caa3e7a9b62a55f4226fa79a94. StyleBro main 241bca8, packed as 0.8.0-trial53.1.
Setup: path B, `bro migrate . --write`, then the `GlobalPackageReference` StyleCop.Analyzers 1.3.149-beta in
Directory.Packages.props replaced by StyleBro.Analyzers. Local workarounds: `git submodule update --init` (ext/MessagePack
sources are compiled into the library), global.json 10.0.402 lowered to 10.0.100 (latestFeature).

## Baseline (untouched)

Debug build green; TWAE Debug/Release green; tests (MTP, 980): 972 + 8 skipped in Debug, 971 + 9 skipped in Release, none
failed. The skip count moves between 8 and 9 from run to run (it did in the untouched runs too).

## Both engines

Migrate: SA1309 on (off in ext/), so BRO1303 uses camelCase (306 of 306 fields); settings into .editorconfig,
ext/.editorconfig, src/Nerdbank.Streams/.editorconfig and test/.editorconfig, preset none, 4 suppressions added for the
replacing rules (2 test files), the workflow's `dotnet format --verify-no-changes` named; it warned that the fresh clone
wasn't restored. preview 0 (0 files, nothing to change), format 0 (6 runs: 5 frameworks, plus net8.0 Release for the
tests' `#if DEBUG`; clean on run 1), check 0 Clean, check --build 0 Clean (Debug, Release; IDE0049 counted as checked:
every project enforces code style in the build); TWAE Debug/Release green; tests as the baseline (971/972, 0 failed).
The submodule's linked files stayed untouched and reported nothing.

Host vs default: trees, exit codes and both --json identical; logs differ in the order of the runs and, in check, PR #287's
load note (the host's net10.0 run).

No findings.
