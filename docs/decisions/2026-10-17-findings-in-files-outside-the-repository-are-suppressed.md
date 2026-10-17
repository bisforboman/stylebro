# Findings in files outside the repository are suppressed (2026-10-17)

## Question

`stylebro-migrate format` leaves files a project compiles from outside its repository alone (owner, 2026-10-16:
`..\..\outside\Shared.cs` is someone else's code, like a submodule's). The build still analyzed them, so `format
--verify-no-changes` said clean while a build with TreatWarningsAsErrors failed on BRO1107, BRO1001, BRO1303 and BRO1008
there (trial 27). What should the build do with those files?

## Choices

1. **Suppress StyleBro's findings there**: a suppressor hides them in files outside the project's git repository, like
   the ones for NuGet packages, git submodules and generator output.
2. Have `format` fix them after all.
3. Leave it: document that the build reports what `format` leaves.

## Decision

**Choice 1** (owner's choice 2026-10-17: "suppress StyleBro findings there"). `OutsideRepositorySuppressor` hides BRO
findings and those of the built-in rules StyleBro's preset, `init` and `migrate` turn on (the baseline's list): `format`
doesn't touch the file for those either. The package's build targets pass `StyleBroRepositoryRoot`, the nearest folder above
the project with a `.git` folder (found by its `HEAD`) or a `.git` file (a worktree or submodule); without it, or with
`<StyleBroRepositoryRoot>none</StyleBroRepositoryRoot>`, nothing is hidden. StyleBro's analyzers skip such files
altogether, like package files. Package files keep their built-in rules' findings (only BRO ones are hidden there, as
before).
