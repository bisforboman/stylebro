# Where trials run (2026-10-17)

## Question

First-run trial agents build, format and test unseen repositories on the owner's desktop machine, several at once. A
Verify-based test once opened a diff window on the desktop, and runs leave processes, locked files and stray props behind.
Where should trial agents run?

## Choices

1. Hybrid containers: library repositories in local Linux containers, Windows-only ones in a Windows container (or on the
   host under a no-desktop rule until Windows containers are switched on).
2. **Hybrid: cloud sessions for Linux repos, host for Windows-only, Docker as fallback.**
3. Keep running everything on the host, with stricter rules only.

## Decision

**Choice 2** (owner, 2026-10-17: first "Hybrid containers, just set it up", then the same day: trials run mainly as cloud
sessions). Library repositories and spot checks run as cloud sessions (Linux). Windows-only repositories (WPF, WinForms,
.NET Framework, old-style csproj, net4x targets) run on the host under the no-desktop rule: no GUI apps, no window,
clipboard, screen or hotkey tests, no diff tools, even on the host.

The Docker setup in `scripts/trials/` stays as the fallback for local reruns and a future Windows container: the Linux
image (`Dockerfile`: .NET SDK 10.0.402 base, SDKs 9 and 8, runtimes 7 and 6, git, pwsh, python3, a non-root user), the
Windows image (`Dockerfile.windows`, untested until Docker runs Windows containers) and `Start-TrialContainer.ps1` (one
container per agent, the trial folder bind-mounted, the feed read-only, the shared `stylebro-nuget` volume, 4 CPUs and
12 GB by default). How trials run: `.trials/README.md`.
