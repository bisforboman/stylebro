# Trials run in containers (2026-10-17)

## Question

First-run trial agents build, format and test unseen repositories on the owner's desktop machine, several at once. A
Verify-based test once opened a diff window on the desktop, and runs leave processes, locked files and stray props behind.
How should trial agents run?

## Choices

1. **Hybrid containers**: library repositories in Linux containers (pinned SDKs, fresh state, a shared NuGet cache volume,
   CPU and memory limits); Windows-only repositories (WPF, WinForms, .NET Framework, old-style csproj, net4x targets) in a
   Windows container, or on the host under a no-desktop rule until Windows containers are switched on.
2. Everything in Windows containers (needs the engine switched, slower, large images).
3. Keep running on the host, with stricter rules only.

## Decision

**Choice 1** (owner's choice 2026-10-17: "Hybrid containers, just set it up"). `scripts/trials/` holds the Linux image
(`Dockerfile`: .NET SDK 10.0.402 base, SDKs 9 and 8, runtimes 7 and 6, git, pwsh, python3, a non-root user), the Windows
image (`Dockerfile.windows`, untested until Docker runs Windows containers) and `Start-TrialContainer.ps1` (one container
per agent: the agent's trial folder and logs bind-mounted, the feed read-only, the `stylebro-nuget` volume, 4 CPUs and
12 GB by default, stylebro-migrate installed from the feed). Agents drive containers with `docker exec`, and clone inside
the container, so results reflect Linux checkouts (the real-world CI already runs several repos on Linux with the same
results). How trials run: `.trials/README.md`. Nothing launches GUI apps, even on the host.
