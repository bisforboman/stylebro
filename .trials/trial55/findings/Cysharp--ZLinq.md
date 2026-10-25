# Cysharp/ZLinq (trial 55, random: entry 60, draw 11)

main f7591df2, 572 .cs (the largest of the four): ZLinq.slnx with the library (netstandard2.0/2.1, net6.0-net10.0), an
in-solution source generator (ZLinq.DropInGenerator, built first), benchmarks (Release runs), sandbox apps, a Unity folder
no project compiles, tests (net8.0-net10.0 + System.Linq.Tests); `Features=strict`, TreatWarningsAsErrors under
ContinuousIntegrationBuild (the runs set CI=true, so every build here is TWAE); several .editorconfig files; no analyzers,
no StyleCop (init); xunit.analyzers 1.24.0.

## Baseline (untouched)

Build, TWAE Debug/Release green; tests 1,079/1,172/1,182/1,182 pass per framework (1-2 skipped), Debug and Release.

## Default engine (host)

- preview init 538 s: 231 files, 1,227 changes, "clean after 1 run" (run 1 7 m 27 s, run 2 60 s), 16 kept findings
  (BRO1311: tuple element names whose other uses are in disabled `#if` code), baseline in the patch.
  **Finding 2 (fixed):** the summary listed "Indentation kept" for `src/ZLinq/Linq/Sum.cs` twice, with
  `21,26,32,142` and `21,26,32-33,36,159`: each preview format pass is its own `bro format` process, and the second one
  numbered the lines of the text the first had fixed (the notes were de-duplicated by exact text only). `bro format` itself
  prints it once (run 1's, the user's own line numbers). Fixed: the preview keeps the first note for the same files.
- init: 7 rules off for mixed code (BRO1106, BRO1133, BRO1001, BRO1404, BRO1007, BRO1604, BRO1306), 4 off as not
  followed (BRO1601 457 of 457, BRO1135, BRO1113, BRO1149), 4 settings "both"; honest counts.
- format 684 s: 12 runs per pass (6 frameworks, Release for Benchmark, shared-file groups for the generator projects);
  run 1 changed 231 files, run 2 clean, exit 0; baseline 16 entries at the root (the solution is at the root).
- check 232 s, check --build 495 s (Debug, Release: "no findings, no new errors"), exit 0.
- Header nit (known, trial 44): on the fresh clone format's first line says "Fixing StyleBro's rules, xunit.analyzers'
  assertion rules ... (161 ids)", the next says xunit.analyzers 1.24.0's fixes have no Fix All and aren't applied; the
  header is written before the restore. check (restored) says 141 ids. Not changed.
- TWAE Debug/Release green; tests identical to the baseline (no failures).
