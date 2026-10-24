# Trial 52 final re-check (0.8.0-trial52.2 = branch claude/trial-52 at 26b60bc, on main 8247120; fresh clones)

Both engines, every repository: preview, setup, format, check, check --build, TWAE Debug/Release, tests.

| Repository | Exit codes (both engines) | TWAE | Tests | Preview vs format |
|---|---|---|---|---|
| error-or | all 0 (tests 1: net462 needs Mono, as untouched) | green | 228 x net8.0/net10.0, Debug and Release | code files identical |
| DbUp | all 0 | green | 100/100 x 2 | identical |
| figgle | all 0 | green | 323 + 30 + 4 x 2 | code files identical |
| EasyNetQ | all 0 | green | 443 + 14 + 3 x 2; Hosepipe flaky as untouched | code files identical |

"Code files identical": only the package-reference files differ, where step 1 was done by hand (the preview keeps
StyleCop's reference next to StyleBro's, as documented, and puts its own reference elsewhere in the file).

Host vs default: trees (git diff, status), exit codes, check/check --build `--json` and the format/check/check --build logs
(timings and clone paths aside) identical on all four. error-or's migration now says: "Line endings: your .editorconfig
says end_of_line = crlf, and 12 of 48 C# files are LF as committed ...". DbUp's init reads src/ (and src/.editorconfig),
pins the preset in src/Directory.Build.props.

Timings (s, default/host): format 77/29, 83/42, 98/57, 143/73; check 19/18, 0/1, 1/1, 39/28 (error-or, DbUp, figgle,
EasyNetQ; the check is format's clean record where it shows 0-1 s). Two chains ran at a time.
