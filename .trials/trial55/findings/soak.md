# Trial 55 engine soak (the host is the default since #303)

Each repository: the trial's steps on fresh clones with no engine named (host) and with `--engine dotnet-format`
(`compare.sh`: exit codes, trees, `--json`, every log normalized, progress and finding lines as sets), plus
`scripts/format-speed/Compare-Format.ps1` on a clone with step 1 committed (check on the unformatted tree, format, recheck).

| Repository | Exit codes | Trees | --json | Logs | Tests/TWAE | Compare-Format |
|---|---|---|---|---|---|---|
| Newtonsoft.Json.Schema | same | same | same | same (times only) | same | Same |
| FeatureManagement-Dotnet | same | same | same | same (NU1510 lines in build order) | same | Same |
| ZLinq | same | same | same | **per-run "files with findings" counts** | same | **DIFFERENT** (same lines) |
| protobuf-net.Grpc | same | same | same (trial steps) | **preview "reported changes" 407/384** | same (flaky integration tests) | **DIFFERENT**: check lists, format --json |

No fallback to dotnet format on the default path (no "Engine: the host can't run here" / "couldn't load" line in any run).
The load-warning note: Json.Schema (SourceLink on the trial clone's local origin) and FeatureManagement (NU1510), the same
on both engines; `--verbosity diagnostic` with the host lists Json.Schema's warning, so #303's Serilog follow-up didn't
reproduce here.

## Difference 1: ZLinq, which frameworks a referenced project loads in

ZLinq.Tests targets net6.0;net8.0;net9.0;net10.0, ZLinq netstandard2.0;netstandard2.1;net8.0;net9.0;net10.0 (no net6.0). The
old engine's "net6.0, part 2" run (ZLinq.Tests) loaded ZLinq for all five of its frameworks (`StyleBroFormatLoose`: a
project without the run's framework asks its references for all of theirs, by design), so its check found BRO1519 in
netstandard2.0-only code (`src/ZLinq/Internal/SingleSpan.cs`, `Internal/Polyfill/ArraySegmentExtensions.cs`, disabled under
net6-9) and listed "71/220/220 files with findings" where the host lists 69/218/218 (net6.0 part 2, net8.0, net9.0; seen
with a debug build listing each run's files, `--verbosity diagnostic` and `check --files` on the two files, where both
engines agree). The netstandard2.0 run fixes those lines on both engines; nothing else differs.

## Difference 2: protobuf-net.Grpc, the host reports findings dotnet format doesn't

On the unformatted tree the host's check lists 1,100 finding lines, dotnet format's 1,072: the 28 extra are 23 `warning
IDE0055` next to the `error WHITESPACE` dotnet format reports at the same place, and 5 BRO findings (BRO1101, BRO1149,
BRO1515, BRO1520, BRO1138) on lines 402-466 of `Internal/ContractOperation.cs` (no `#if` there), all in
protobuf-net.Grpc.csproj. dotnet format re-analyzes after each fix (its whitespace pass first), so a finding on text an
earlier fix changed isn't reported; the host's analysis seems to keep them (AnalysisCache). The same shows in format's
`--json` (`changesPerRule.IDE0055` 54 vs 31) and the preview's "reported changes" (407 vs 384, the same on a second run of
each engine). Trees, exit codes and verdicts are the same.

## Times (s, host / dotnet-format)

| Repository | preview | format | check | check --build | CF check | CF format | CF recheck |
|---|---|---|---|---|---|---|---|
| Newtonsoft.Json.Schema | 169 / 618 | 203 / 834 | 128 / 185 | 513 / 610 | 285 / 771 | 181 / 771 | 144 / 233 |
| ZLinq | 538 / 1084 | 684 / 1308 | 232 / 251 | 495 / 602 | 765 / 1193 | 593 / 1289 | 135 / 214 |
| FeatureManagement-Dotnet | 93 / 160 | 92 / 230 | 55 / 79 | 119 / 129 | 80 / 248 | 73 / 142 | 50 / 53 |
| protobuf-net.Grpc | 275 / 559 | 235 / 790 | 185 / 228 | 385 / 431 | 258 / 760 | 291 / 716 | 138 / 300 |

Two chains ran in parallel on 4 cores (load ~25), so the times compare the engines, not machines.
