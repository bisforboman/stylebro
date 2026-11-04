# Trial 67 progress
- feed 0.8.0-trial67.1 packed (/root/stylebro-trial67/feed), bro installed globally
- picks: Dumpify (109), RestEase (110), CQRSlite (111) [Gridify skipped: net11.0]; targeted moq4; extra AspNetCoreRateLimit
- CQRSlite baseline: Debug build ok 0 warn, 126 tests pass (MTP: dotnet test --solution CQRSlite.sln; do NOT set DOTNET_ROLL_FORWARD for it: handshake failure exit 5)
RestEase baseline: 10 failing header tests per framework (CRLF expected, Linux) x4 frameworks; net452 not run; HttpClientFactory 4, SourceGenerator 186 pass (DOTNET_ROLL_FORWARD=Major)
- CQRSlite r1 (init + manual PackageReference via central package mgmt; clean): format 35s (60 files changed), format2 21s, check 22s, check --build 75s, all exit 0 / Clean; TWAE Debug+Release (no-incremental) pass untouched and after; tests 126 same.
  FINDING A (perf, not verdict): CQRSlite's record has no #checked so the fast check never engages (check 18-22 s, Dumpify 1 s). Cause: FormatCommand.cs:5037 `gaps.All(g => IsHeldBackGap(g) || IsRawStringGap(g))` rejects the xunit.analyzers note gap (Coverage.cs XunitGap, a verdict-neutral note like HeldBack) -> checkedClean false -> SaveKept without #checked. Fix: treat any NoteEnd gap as OK and put it in the #verdict lines. Not fixed yet.
- Dumpify r1: init + refs + nuget.config feed; format 65s (81 files), format2 26s, check 1s (fast), checkjson 0s, check --build 66s, all exit 0 (verify details later)
- scripts: env.sh base.sh run.sh twae.sh rec.sh in .trials/trial67 (record path /tmp/stylebro-kept/<hash>.tsv, rec.sh gets hash wrong for Dumpify - check)
