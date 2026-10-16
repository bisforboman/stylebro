# Files outside the repository are left alone (2026-10-16)

## Question

Trial 26: a project compiled a file from outside the repository (`<Compile Include="..\..\..\outside\Shared.cs"
Link="..." />`). `format` rewrote it silently: it wasn't in the list of changed files and not in the preview. What should
`format` do with such files?

## Choices

1. Leave them alone, like git submodules, and say so: "Leaving alone: ../outside/Shared.cs (outside the repository)".
2. Format them and list them with the changed files.

## Decision

**Choice 1: leave them alone** (owner's choice, 2026-10-16). `format` names them once, puts them back after each run if a
fix reached them, and their findings don't count in the checks or in `--verify-no-changes`.
