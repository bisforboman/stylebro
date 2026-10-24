# shibayan/Sharprompt (trial 53, random entry 55, draw 39)

Commit 5e17ace494e7e7a87cd0daa3e3481eead749a68f. StyleBro main 241bca8, packed as 0.8.0-trial53.1.
Setup: path A with central package management (PackageVersion in Directory.Packages.props, a version-less reference in
the root Directory.Build.props), then `bro init --write`. No local workaround.

## Baseline (untouched)

Debug build green; TWAE Debug/Release green; tests (MTP) 1,352 passed in Debug and Release.

## Both engines

preview 0 (15 files, 49 reported changes, clean after 1 run), init 0, format 0 (15 files on run 1, clean on run 2; the
preview matched it file for file), check 0 Clean, check --build 0 Clean (Debug, Release); TWAE Debug/Release green; tests
1,352 x 2. 18 files changed in all (15 C# + .editorconfig, Directory.Build.props, Directory.Packages.props).
Notes that held: multi-targeted projects, the Microsoft-style hint, `csharp_preserve_single_line_statements = false` vs
one-line empty blocks, the workflow's `dotnet format --verify-no-changes`; `tests/Sharprompt.NativeAot/Directory.Build.props`
(no StyleBro there) named for EnforceCodeStyleInBuild only.

Host vs default: trees, exit codes and both --json identical; the logs differ only in the order of the per-framework runs
("Checked net8.0 (run 2 of 3)" / "net10.0 (run 3 of 3)" swapped) and timings. Format 152 s default, 78 s host.

No findings.
