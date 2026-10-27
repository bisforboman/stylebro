# Fixture matrix

Tiny repositories in the shapes that kept breaking first-run trials (owner's choice, 2026-10-27: "fixture matrix first, then
trials"), each run end to end through the first-run path a user takes, with StyleBro.Tool and StyleBro.Analyzers packed
from this checkout. Every trial fix used to become a one-off unit test; a shape here covers the whole CLASS of repository,
so a new trial repository stops finding variants of the same bug.

```
pwsh scripts/fixtures/Invoke-Fixtures.ps1                    # every shape (about 3-5 minutes each)
pwsh scripts/fixtures/Invoke-Fixtures.ps1 -Shape encoding,two-paths -Keep
pwsh scripts/fixtures/Invoke-Fixtures.ps1 -List
pwsh scripts/fixtures/Invoke-Fixtures.ps1 -Shard 2 -Shards 6 # what one CI shard runs
```

`-Feed <folder> -Version <v>` reuses packages packed earlier (both StyleBro packages and `packages/*`); without them the
runner packs everything as `0.0.0-fixture.<time>`, so the global packages folder never serves a stale build. `-Work`
(default: a new temp folder) must be outside the repository. `-Keep` keeps passing shapes' clones; a failing shape's are
always kept, its logs are in `<work>/<shape>/logs/`. In a cloud container set `NUGET_CERT_REVOCATION_MODE=offline` first.

## What each shape goes through

1. `bro preview init` (`preview migrate` for a StyleCop shape) on the checkout, before anything references StyleBro;
2. the package reference (getting-started's step 1, or the shape's `Reference`: where that repository's users would put
   it) and `bro init --write`, or `bro migrate --write` and then the package swap, committed;
3. on one clone per engine (the host, the default, and `--engine dotnet-format`): `bro format`, `bro check`,
   `bro check --build`, each with `--json`;
4. a `TreatWarningsAsErrors` build of the formatted code (`--no-incremental`) and the fixture's own tests.

And asserts:

- **honest verdicts**: every exit code is the one the shape expects (`Expect`, default 0); 0 only with a clean verdict and
  nothing in the report's `notChecked`, 3 only with "Not verified" and what wasn't checked (`Reasons`: a pattern the output
  must match); no analyzer crash (AD0001) or exception;
- **one run**: no format run after the first changes a file, the check afterwards finds nothing, and neither the check nor
  check --build changes the tree;
- **preview = format**: the preview's patch, applied to the untouched checkout, changes the same C# files and baselines as
  init + format, with the same text;
- **builds and tests** pass after format (a shape whose untouched build fails says why in `BuildFails`);
- **both engines** give identical trees, exit codes and `--json` reports (timings and output aside);
- the shape's own `Assert` (a generated file left alone, a .vb file untouched, ...).

## Writing a shape

One file per shape in `shapes/`, returning a hashtable; its header comment names the trials it comes from and what broke.

| Key | Meaning |
| --- | --- |
| `Description` | one line for the log |
| `Create` | `{ param($root, $context) ... }` writes the files (helpers in `Fixtures.psm1`: `Set-Text` with `-Crlf`/`-Bom`/`-NoFinalNewline`, `Set-Messy`, `New-Project`, `New-TestProject`, `New-Solution`, `Save-Commit` for history). The runner writes `nuget.config` (the local feed + nuget.org) and `.gitignore` first and commits after |
| `Shallow` | clone with `--depth 1` |
| `Before` | `{ param($repo, $context) }` on the checkout and each engine's clone before StyleBro runs (a build, a restore) |
| `Migrate` | path B: `preview migrate`, `migrate --write`, then `Reference` swaps the package |
| `Reference` | `{ param($repo, $context) }` adds StyleBro.Analyzers `$context.Version` (default: the root Directory.Build.props) |
| `Args`, `PreviewArgs` | extra arguments for format/check and for the preview (a solution filter, `--project`) |
| `Expect`, `Reasons` | exit codes and output patterns per step: `preview`, `format`, `check`, `check-build` (the preview defaults to format's) |
| `EngineExpect`, `EngineReasons` | the same per engine (`@{ 'dotnet-format' = @{ format = 3 } }`), where a decision makes the engines differ |
| `EnginesDiffer` | why the engines' trees and reports may differ (skips that comparison; the per-engine expectations still hold) |
| `InitReason` | a pattern init's (or migrate's) output must match: a note it must give |
| `Target`, `BuildArgs` | what the TWAE build and the tests run on (default: the root) |
| `BuildFails` | the reason the repository's own build fails, before and after StyleBro |
| `Tests` | `$false` when the fixture has no tests |
| `Assert` | `{ param($formatted, $runs, $context) }` returns problems as strings |

`packages/` holds helper packages the runner packs into the same feed with the same version (`Fixture.Trimmer`: build/
props and targets shaped like ReferenceTrimmer's).

Keep fixtures tiny (a project or three, a few files): each shape costs about four minutes of CI, mostly builds.

## CI

`.github/workflows/fixtures.yml`: six shards (shape index modulo 6), the job `fixtures` is the required check
([decision](https://github.com/bisforboman/stylebro/blob/main/docs/decisions/2026-10-27-the-fixture-matrix-is-a-required-check-on-src-prs-and.md));
it does the work when a pull request changes src/, the fixtures or the build settings (`.github/changes.sh`), and nightly
on main. A failing shard uploads the shapes' logs.

## Trials

A trial that finds a new repository shape adds a shape here, not only a unit test for the fix (`.trials/README.md`).
