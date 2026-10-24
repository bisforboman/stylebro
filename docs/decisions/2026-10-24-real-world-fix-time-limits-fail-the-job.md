# Real-world fix-time limits fail the job (2026-10-24)

## Question

The real-world jobs time the `dotnet format` runs together and compared them with a provisional `MaxFixSeconds` per repo
(scripts/realworld/repos.psd1), warning only. With the fix-side speed fixes in (PRs #277, #278, #280), what should the
limits be, should going over them fail, and does the BroFormat step (`bro init --write` + `bro format`, Newtonsoft.Json)
need its own limit?

Measured: 13 runs per repo on its own runner (the nightlies of 2026-10-23 and -24 and the latest run of 11 PRs, all
after #280). Runs vary about 2x from run to run, but the slowest was at most 1.27x its repo's median (FFMpegCore;
OpenTelemetry 1.23x). Proposed limits: about 2x the median, rounded up.

| Repo | Median (s) | Range (s) | Old value | Limit |
|---|---|---|---|---|
| CsvHelper | 273 | 147-303 | 480 | 550 |
| FFMpegCore | 44 | 25-56 | 120 | 90 |
| FluentValidation | 120 | 60-127 | 270 | 240 |
| Jellyfin | 230 | 141-254 | 540 | 460 |
| LibGit2Sharp | 230 | 124-250 | 390 | 460 |
| Mapperly | 89 | 55-96 | 150 | 180 |
| MapperlyCSharpier | 223 | 135-248 | 420 | 450 |
| MediatR | 74 | 62-86 | 180 | 150 |
| Mediator | 415 | 247-459 | 840 | 830 |
| Newtonsoft | 887 | 508-1009 | 2700 | 1800 |
| OpenTelemetry | 395 | 332-484 | 960 | 800 |
| Polly | 363 | 221-393 | 600 | 730 |
| Serilog | 175 | 98-205 | 400 | 350 |
| eShopOnWeb | 49 | 30-58 | 120 | 100 |

The BroFormat step had only 2 timed runs (1162 s and 2401 s).

## Choices

Over the limit:

1. **Fail on every repo** (owner's pick), with a per-repo `FixTimeWarnOnly = $true` to go back to a warning.
2. Fail, but FFMpegCore and OpenTelemetry (the noisiest) warn only.
3. Keep warning until about 5 more nightlies have run.

The BroFormat step:

1. **A warning-only limit** (owner's pick): `MaxBroFormatSeconds = 3600` on Newtonsoft, made a failure once nightlies
   show its median.
2. A failing limit now.
3. No limit.

## Decision

The limits above fail the job (the message names the repo, the time and the limit); `FixTimeWarnOnly` per repo turns one
back to a warning. Newtonsoft's `bro format` step gets `MaxBroFormatSeconds = 3600`, warning only.
