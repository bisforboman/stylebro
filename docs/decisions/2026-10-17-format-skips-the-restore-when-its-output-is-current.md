# Format skips the restore when its output is current (2026-10-17)

## Question

`stylebro-migrate format` runs `dotnet restore` before every command, also when the solution is already restored (2-18 s
per call, PalCalc 17.5 s). Should it skip it
([proposal](https://github.com/bisforboman/stylebro/blob/main/docs/proposals/format-speed.md), Q-D4)?

## Choices

1. **Skip `dotnet restore` when the restore output is newer than every input and lists StyleBro.Analyzers**: before 2-18
   s per call, after ~0 when nothing changed; `--restore` forces it. Cost: a file-time check can miss a change NuGet's
   own check would catch (a package replaced under the same version).
2. Always restore (today): always right, 2-18 s per call.
3. Restore only with `--restore`: fastest, but a first `format` right after adding the package would run without it.

## Decision

**Choice 1** (owner's choice 2026-10-17). Implemented in phase 0: the restore is skipped when every SDK-style project's
`project.assets.json` and `project.nuget.cache` exist, the restore succeeded, every package file it expects is in the
packages folder, the assets list StyleBro.Analyzers, and they are newer than every MSBuild, NuGet and `global.json` file of
the repository and the folders above it, and the user's NuGet.Config. `--restore` forces it, `--no-restore` still skips it.
