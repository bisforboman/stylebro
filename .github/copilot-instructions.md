# GitHub Copilot

Read [AGENTS.md](../AGENTS.md) at the repository root first: it holds the instructions for every agent working on this
repository (layout, build and test, the checks before pushing, the design rules for every rule). The details behind
it are in [CONTRIBUTING.md](../CONTRIBUTING.md).

Before pushing, run what CI runs:

```
dotnet test StyleBro.slnx -p:StyleBroSelf=none
pwsh scripts/verify-format.ps1
dotnet format StyleBro.slnx --verify-no-changes --severity warn --exclude samples
```
