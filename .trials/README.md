# Trials

First-run trials clone public repositories here (`.trials/trialNN/<repo>`, repros in `.trials/trialNN/spot/`, logs in
`.trials/trialNN/logs/`). Everything except this README and the stub files is gitignored.

The stubs make a clone see a clean machine: MSBuild and `.editorconfig` lookups walk up the folder tree, and without
them every clone would inherit StyleBro's own `.editorconfig`, `Directory.Build.props/.targets` and
`Directory.Packages.props` (an early real-world run failed to build that way). A clone's own files still win.

## Choosing the repositories (owner's decision, 2026-10-17)

Each trial takes four repositories: three targeted at what the previous trial's fixes changed, one random.

Every repository must:

- be public on GitHub and never used before, neither in a trial nor in the real-world CI (`scripts/realworld/repos.psd1`);
  check the earlier `.trials/trialNN` folders and grep the git history (commit messages and diffs) for the name, since
  trials before 23 lived outside this repository and only their fixes record them;
- build on the trial machine with the installed SDKs (a small local workaround is fine, say so in the report; a repo
  that needs an SDK or target framework that isn't installed is out);
- have tests that run offline (leave out ones needing Docker, a network or a database, and say which);
- have tens to a few hundred C# files;
- together with the other three give a mix of setups (StyleCop / StyleSharp / Sonar / CSharpier / none, single- or
  multi-targeted, warnings as errors, apps and libraries, XAML, generators).

Procedure:

The lists live in [`scripts/trials/`](../scripts/trials/README.md) (owner's decision, 2026-10-19): `used.tsv` (everything
used or rejected), `pool/` (checked candidates, tagged) and `random.json` (the fixed random list).

1. Targeted: for each area the last trial's fixes touched, pick a repository that exercises it (for example a XAML MVVM
   app for name guards, a multi-targeted library with `#if` in method bodies for format, a CSharpier repository with an
   in-solution generator). Take a checked pool entry with the right tags first; otherwise smoke-build new candidates
   (restore + Debug build) and add them to the pool, checked or rejected.
2. Random: pick from `scripts/trials/random.json` with Python's `random.Random(<trial number>).randrange(len(repos))`.
   If the pick is in `used.tsv` or fails the practical criteria above, take the next entry down the list (wrapping
   around) and write down why. Log the seed and the pick in `.trials/trialNN/logs/`.
   **Variant: three random plus one targeted** (owner's choice, 2026-10-23, "Mostly random": closer to what a new user
   meets than re-probing the last trial's edge cases), allowed instead of the three-targeted-one-random split: draw three
   with ONE generator, `rng = random.Random(<trial number>)` and `rng.randrange(len(repos))` three times. A draw that is in
   `used.tsv`, already picked in this trial or fails the criteria moves down the list (wrapping). If the list runs out of
   usable entries, append new candidates at the end (popular C# repositories, not huge, not Windows-only, not in
   `used.tsv`) and keep walking from the draws made with the old length; log the length, the indices and the picks. The
   fourth repository is targeted at the last trial's fixes (a pool entry if one fits).
3. Record each repository's commit, and why it was chosen, in the trial's logs; in the trial's PR add the four to
   `used.tsv` (and set `usedIn` on their pool entries).
4. **A fixture for every new shape** (2026-10-27): a finding that comes from the repository's SHAPE (git state, committed
   or generated files, line endings and encodings, multi-targeting, settings files and solution layout, ...), not from one
   rule's code, adds a shape to the fixture matrix ([`scripts/fixtures/`](../scripts/fixtures/README.md)) in the fix's PR,
   besides the unit test: a tiny repository that fails before the fix and passes after it, run with
   `pwsh scripts/fixtures/Invoke-Fixtures.ps1 -Shape <name>`. A rule's own bug (a fix order, a rename) stays a unit test,
   FixOrderTests case or Messy sample. Before a trial, the matrix runs green on the commit tested (CI's `fixtures` check).
5. When the owner has given the verdict, the trial's PR adds its `## Trial NN` section (repositories, fixes, verdict and
   tally, environment notes) at the end of `docs/notes/trials.md` (not CLAUDE.md, since 2026-10-23).

## Where trials run (owner's decision, 2026-10-17)

"Hybrid: cloud sessions for Linux repos, host for Windows-only, Docker as fallback"
([decision](https://github.com/bisforboman/stylebro/blob/main/docs/decisions/2026-10-17-where-trials-run.md)):

| Repository | Where |
| --- | --- |
| Libraries, console/web apps, anything that builds on Linux (net6.0-net10.0, netstandard); spot checks | Cloud sessions (Linux) |
| WPF, WinForms, .NET Framework / net4x targets, old-style csproj, Windows-only tests | The host, under the no-desktop rule |

Nothing a trial does may touch the owner's desktop: no GUI apps, no window, clipboard, screen or hotkey tests, no diff
tools (`DiffEngine_Disabled=true`, `CI=true` for Verify suites), even on the host. Host runs keep the old rules: own tool
path, `NUGET_PACKAGES` per trial, never `dotnet build-server shutdown`, don't kill processes you didn't start.

## Docker fallback (local reruns, a future Windows container)

`scripts/trials/` is the fallback for local reruns and a future Windows container: one container per agent, made by
`scripts/trials/Start-TrialContainer.ps1`, so nothing is left on the host (processes, locked files, stray props).
`Dockerfile` is the Linux image; `Dockerfile.windows` (Windows-only repos) needs Docker in Windows-containers mode, the
owner's setting, and is untested until then (`-Windows` refuses otherwise).

Tested 2026-10-17 with CliFx at 10cbf9b6 (StyleBro main 12f01ef0, 4 CPUs/12 GB): init, format (clean on run 2), verify
clean, Debug/Release TreatWarningsAsErrors builds with and without EnforceCodeStyleInBuild green, 122/122 tests before and
after, no AD0001; the same as trial 27 on the host apart from that trial's own fixes. Restore 40 s with an empty NuGet
volume, 5 s warm. The bind mount is slow: a Debug build took 25 s on `/work` and 11 s on the container's own file system,
and format ~6.5 minutes. For speed, clone under `/home/trial` and copy logs to `/logs`. Image ~2.3 GB.

The Linux image: .NET SDK 10.0.402 (the base), SDKs 9.0.319 and 8.0.426, runtimes (and ASP.NET Core runtimes) 7.0.20
and 6.0.36, git, pwsh, python3, user `trial`, nothing GUI. Raise the pinned versions together with the trial machine's.

### Commands (on the host, from the repository root)

```powershell
# The feed, as for every trial: pack StyleBro.Package and StyleBro.Migrate (package StyleBro.Tool) with -p:Version=0.8.0-trialNN -o $env:TEMP\stylebro-trialNN\feed
./scripts/trials/Start-TrialContainer.ps1 -Trial 29 -Name clifx          # build the image, start stylebro-trial29-clifx
docker exec -w /work stylebro-trial29-clifx git clone https://github.com/Tyrrrz/CliFx .
docker exec -w /work stylebro-trial29-clifx bro init --write
docker exec -w /work stylebro-trial29-clifx pwsh -c '...'               # anything longer: a script under /work or /logs
./scripts/trials/Start-TrialContainer.ps1 -Trial 29 -Name clifx -Stop    # rerun the first line to resume
./scripts/trials/Start-TrialContainer.ps1 -Trial 29 -Remove              # every container of the trial, when it's done
```

Run `docker exec` from PowerShell; from Git Bash set `MSYS_NO_PATHCONV=1` first, or it rewrites `/work/...` into
`C:/Program Files/Git/work/...`.

Inside the container:

- `/work` = `.trials/trialNN/<Name>` (or `-Folder <path>`, e.g. `spot` for spot agents), `/logs` = `.trials/trialNN/logs/<Name>`,
  both read-write and visible on the host;
- `/feed` = the package feed (read-only): the repo's `nuget.config` gets `<add key="stylebro" value="/feed" />`;
- `/nuget` = `NUGET_PACKAGES`, the named volume `stylebro-nuget` shared by all containers (warm restores);
- `bro` (the trial's version) is on the PATH; `DOTNET_CLI_TELEMETRY_OPTOUT`, `DiffEngine_Disabled` are set.
- Limits: 4 CPUs and 12 GB per container by default (`-Cpus`, `-Memory`); the machine has 16 logical CPUs and 64 GB, so
  four agents at a time leave room for the owner's work. Docker Desktop's own VM limit (Settings > Resources) caps them all.
- Inside a container, `dotnet build-server shutdown` and killing processes only affect that agent; on the host the old
  rules still hold (never shut down build servers, don't kill what you didn't start).

Clone INSIDE the container (`git clone` into `/work`), not on the host: the repository's `.gitattributes` then apply as on
Linux (LF checkouts unless it says otherwise), so results reflect a Linux checkout. The real-world CI already runs several
repositories on Linux with the same results as on Windows; a finding that depends on CRLF needs a host or Windows run.
The mount is case-insensitive (it's an NTFS folder): a repository with paths differing only in case can't be cloned there.

Each repro and scratch copy still needs its own `.git` (`git init` + a commit, or a clone) before StyleBro runs: init climbs
to the git root, and a repro inside the agent's clone without its own `.git` would be judged as part of that clone.

A brief for a container run (written per trial, not committed) must give the agent its container name and the
Start/Remove lines, say that every command runs through `docker exec` (paths `/work`, `/logs`, `/feed`), to clone inside
the container, and the no-desktop rule.

## Engine soak (until the next release)

Since 2026-10-25 the in-process host is `bro format`'s default engine
([decision](../docs/decisions/2026-10-25-the-format-host-becomes-the-default-engine.md)); `--engine dotnet-format` is the old
one. Until the next release, every trial still runs its soak on both: `scripts/format-speed/Compare-Format.ps1` on each
repository (it names both engines, `check`, `format`, `check`), plus `bro check --build` with each engine. A difference
between the engines makes the soak not clean (decision of 2026-10-23); the trial's own commands use the default (host).
Allowed differences: the load-warning note (compared unordered), the old engine's loose load of a referenced project, C#
projects that reference a Visual Basic project (Not checked on the old engine), and the old engine's duplicate finding lines
from a second instance of a multi-targeted referenced project in `check` on unformatted code (compare the distinct findings
as sets; [decision](../docs/decisions/2026-10-29-the-old-engines-duplicate-findings-from-a-second-project.md), trial 60).
