# init and migrate write into the project folders when the build copies the root editorconfig (2026-10-21)

## Question

Trial 40, SixLabors/ImageSharp.Web (StyleCop, migrate): the root .editorconfig is a copy of shared-infrastructure/.editorconfig
(a git submodule), and the repository's build copies it there again before every build (SixLaborsCopyConfigFiles,
BeforeTargets=Build). migrate wrote its 188-line block into the root file; format and check said Clean, then the first build
put the submodule's file back, the block was gone and the TWAE builds reported 897 findings (`check --build` said Not clean,
honestly). The root file is byte-identical to the submodule's. What should migrate and init do when the root .editorconfig is
identical to a file inside a submodule?

## Choices

1. **Write into the folders** (owner's pick): detect it and write the block into a new .editorconfig in each top-level folder
   holding projects (src/, tests/, samples/; no root = true), which adds to the root's. Say why in the report.
2. Stop and explain: write nothing and name the copy; the user moves the settings into the submodule's file or turns the copy off.
3. Write and warn: keep writing the root file, and warn that the build overwrites it.

## Decision

Write into the folders. `Migration.Synced` detects it (the root .editorconfig's text equals an .editorconfig inside a
submodule, and every .csproj lies in a top-level folder); migrate's plan and init's targets write the block, rebased per
folder (`Migration.Rebase`: a section for that folder loses its prefix, one for another folder goes), into
`<folder>/.editorconfig`, and say so (`Migration.SyncedLine`). `Migration.BlockFile` finds the block there for later runs
(recorded StyleCop version and settings, unformatted folders, kept usings; `InitCommand.IsMigrated`).
