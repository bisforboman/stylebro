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

1. Targeted: for each area the last trial's fixes touched, pick a repository that exercises it (for example a XAML MVVM
   app for name guards, a multi-targeted library with `#if` in method bodies for format, a CSharpier repository with an
   in-solution generator). Smoke-build the candidates (restore + Debug build) before choosing.
2. Random: write a fixed list of about 30 popular .NET repositories ranked by GitHub stars (C#, libraries or apps;
   leave out huge ones such as dotnet/runtime, roslyn or aspnetcore, and any already used), then pick one with
   Python's `random.Random(<trial number>).randrange(len(list))`. Log the list, the seed and the pick in
   `.trials/trialNN/logs/`. Only if the pick fails the practical criteria above, take the next entry down the list
   (wrapping around) and write down why.
3. Record each repository's commit, and why it was chosen, in the trial's logs.

## Where trials run (owner's decision, 2026-10-17: containers)

Nothing a trial does may touch the owner's desktop: no GUI apps, no window, clipboard, screen or hotkey tests, no diff
tools (`DiffEngine_Disabled=true`), even on the host. Trials also leave nothing behind on the host (processes, locked
files, stray props). So each trial agent works in its own container, made by `scripts/trials/Start-TrialContainer.ps1`:

| Repository | Where |
| --- | --- |
| Libraries, console/web apps, anything that builds on Linux (net6.0-net10.0, netstandard) | Linux container (`scripts/trials/Dockerfile`) |
| WPF, WinForms, .NET Framework / net4x targets, old-style csproj, Windows-only tests | Windows container (`Dockerfile.windows`) once Docker runs Windows containers; until then on the host, under the no-desktop rule |

The Linux image: .NET SDK 10.0.402 (the base), SDKs 9.0.319 and 8.0.426, runtimes (and ASP.NET Core runtimes) 7.0.20
and 6.0.36, git, pwsh, python3, user `trial`, nothing GUI. Raise the pinned versions together with the trial machine's.

### Commands (on the host, from the repository root)

```powershell
# The feed, as for every trial: pack StyleBro.Package and StyleBro.Migrate with -p:Version=0.8.0-trialNN -o $env:TEMP\stylebro-trialNN\feed
./scripts/trials/Start-TrialContainer.ps1 -Trial 29 -Name clifx          # build the image, start stylebro-trial29-clifx
docker exec -w /work stylebro-trial29-clifx git clone https://github.com/Tyrrrz/CliFx .
docker exec -w /work stylebro-trial29-clifx stylebro-migrate init --write
docker exec -w /work stylebro-trial29-clifx pwsh -c '...'               # anything longer: a script under /work or /logs
./scripts/trials/Start-TrialContainer.ps1 -Trial 29 -Name clifx -Stop    # rerun the first line to resume
./scripts/trials/Start-TrialContainer.ps1 -Trial 29 -Remove              # every container of the trial, when it's done
```

Inside the container:

- `/work` = `.trials/trialNN/<Name>` (or `-Folder <path>`, e.g. `spot` for spot agents), `/logs` = `.trials/trialNN/logs/<Name>`,
  both read-write and visible on the host;
- `/feed` = the package feed (read-only): the repo's `nuget.config` gets `<add key="stylebro" value="/feed" />`;
- `/nuget` = `NUGET_PACKAGES`, the named volume `stylebro-nuget` shared by all containers (warm restores);
- `stylebro-migrate` (the trial's version) is on the PATH; `DOTNET_CLI_TELEMETRY_OPTOUT`, `DiffEngine_Disabled` are set.
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

Briefs (written per trial, not committed) must give each agent: its container name and the Start/Remove lines, that every
command runs through `docker exec` (paths `/work`, `/logs`, `/feed`), to clone inside the container, the no-desktop rule,
and, for host runs of Windows-only repos, the old host rules (own tool path, `NUGET_PACKAGES`, `DiffEngine_Disabled=true`,
`CI=true` for Verify suites).
