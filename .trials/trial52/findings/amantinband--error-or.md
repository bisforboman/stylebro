# amantinband/error-or (trial 52, random entry 50)

Commit a1064860ca999a0cbb1b6fdac6d9ccfd33070e9a. StyleBro main 8247120 (after #297), packed as 0.8.0-trial52.1.
Setup: path B. `bro migrate . --write`, then step 3 (StyleCop.Analyzers -> StyleBro.Analyzers in Directory.Build.props, its
version in Directory.Packages.props).

## Baseline (untouched)

Debug build green; TWAE Debug/Release green; tests 228/228 on net8.0 and net10.0 (Debug and Release); the net462 run fails
to start (no Mono), before and after.

## Default engine, first round

- preview migrate 0 (135 s), migrate --write 0, format 0 (clean on run 2, 13 files + 3 suppression edits + 12 whole-file
  CRLF conversions, see below), check 0 Clean, check --build 0 Clean, TWAE Debug/Release green, tests 228/228 x 2 (net462
  as before). `bro migrate` on a fresh clone warns "Not restored ... run 'dotnet restore', then this again" (honest).

## Finding 2: the migration silently converts 12 files' line endings (also init)

The repository's `.editorconfig` says `[*] end_of_line = crlf`; its C# files are 36 CRLF and 12 LF as committed (no
.gitattributes, git converts nothing). StyleCop never enforced it; the migration turns IDE0055 on, so `bro format` rewrote
all 12 LF files to CRLF: 12 whole-file diffs (about 1,000 of the 1,800 changed lines), and neither `bro migrate` nor
`bro init` says so. The files' majority (75 %) agrees with the setting, so `Conventions.ContradictedEnding` (the "files
decide" note, owner's decision 2026-10-12 "Keep endings out of it, warn") returns null and nothing is reported. The
CleanArchitecture/DynamicData cases (setting against the majority) are reported; this one (setting with the majority,
a minority converted) isn't. The tests still pass (no multi-line literals among the 12).
