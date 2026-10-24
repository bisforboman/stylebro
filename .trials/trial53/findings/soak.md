# Trial 53: format host soak

StyleBro main 241bca8 (after #300; the host code of #298/#299 unchanged), 0.8.0-trial53.1, every repository on a fresh
clone per engine (`run.sh <key> <clone> default` and `... host --engine host`), compared with `compare.sh`.

| Repository | Trees | Exit codes | --json (check, check --build) | Logs |
|---|---|---|---|---|
| BaGet | identical | identical | identical | the preview's copy folder name only |
| Sharprompt | identical | identical | identical | order of the per-framework runs, timings |
| Autofac.Extensions.DependencyInjection | identical | identical | identical | order of the runs, timings |
| Nerdbank.Streams | identical | identical | identical | order of the runs; in check, PR #287's load note on the host's net10.0 run |

TWAE builds and tests: the same on both engines (and as the untouched code). The fixes of this trial (extracted-package
check, charset/final-newline notes) aren't in the host code; BaGet re-run on 0.8.0-trial53.2 (default engine) gave the same
tree and tests, with the charset note.

Timings (s, default/host): format 63/57, 152/78, 58/38, 108/67; check 1/0 (cached), 49/38, 44/34, 65/44; check --build
60/98, 173/154, 408/541, 200/167 (BaGet, Sharprompt, Autofac.Extensions.DependencyInjection, Nerdbank.Streams; two
repositories ran in parallel on 4 CPUs, so the build timings are noisy).
