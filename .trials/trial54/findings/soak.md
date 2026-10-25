# Trial 54: format host soak

StyleBro main dc107c4 (after #301; the host code of #298/#299 unchanged), 0.8.0-trial54.1, every repository on a fresh
clone per engine (`run.sh <key> <clone> default` and `... host --engine host`), compared with `compare.sh`. Dapper again on
0.8.0-trial54.2 (this trial's fixes) with both engines.

| Repository | Trees | Exit codes | --json (check, check --build) | Logs |
|---|---|---|---|---|
| Finbuckle.MultiTenant | identical | identical | identical | the preview's copy folder name, timings |
| Sleet | identical | identical | identical | the preview's copy folder name |
| Flurl | identical | identical | identical | order of the per-framework runs; PR #287's load note (net461, default engine) |
| Dapper, 0.8.0-trial54.1 | **different** | different | different | the default engine's load bug (below) |
| Dapper, 0.8.0-trial54.2 | identical | identical | identical | order of the 17 runs only (sorted, the logs are identical) |

Dapper's first round: the default engine couldn't load Dapper (and so Dapper.Tests, Dapper.Tests.Performance,
Dapper.Rainbow) in its net481, net472 and net5.0 runs, because NuGet's props saw the environment's framework and its targets
the cleared one (ReferenceTrimmer's task assembly empty): "Not verified", exit 3, and 8 findings per configuration left in
the net472 code that `check --build` reported (exit 2). The host engine sets no framework in the environment (it loads every
framework and picks the run's projects), loaded them all, formatted 3 more files and ended clean (exit 0; check --build 3,
because Dapper's untouched builds fail on ReferenceTrimmer's RT0000). The host was right; the fix is in the default engine's
hook (`SelectFrameworkProps`). After it both engines agree byte for byte.

TWAE builds and tests: the same on both engines and as the untouched code (Finbuckle 607/607, Sleet 1,881 + the 1 failure
it has untouched, Flurl 521/547 with the 26 network tests failing untouched, Dapper 8/8 and the 65 tests that pass without
database servers, by name; Dapper's TWAE builds fail on the same 5 RT0000 errors as the untouched code).

Timings (s, default/host): format 96/89, 138/96, 117/49, 429/109 (Finbuckle, Sleet, Flurl, Dapper on 0.8.0-trial54.2);
check --build 66/94, 248/223, 84/91, 172/145. Two repositories ran in parallel on 4 CPUs, so the timings are noisy.
