# The bro command in StyleBro.Tool (2026-10-18)

## Question

Can the tool's commands be snappier than `stylebro-migrate init --write`, `stylebro-migrate [path] --write`,
`stylebro-migrate format --verify-no-changes` and `stylebro-migrate init --diff`? Which package should hold the
command, and when should the rename land?

## Choices

Command:

1. **`bro` with subcommands** (owner's pick): `bro init`, `bro migrate` (the StyleCop path, until now the bare path),
   `bro format`, `bro check` (= `format --verify-no-changes`), `bro preview <init|migrate|format>` (= `--diff`), `bro
   baseline`; the old option forms keep working. `bro` matches the BRO rule prefix. Other programs named `bro` exist
   (Zeek's old binary, the Ruby gem "bro pages"): PATH order decides.
2. `bro` with today's arguments.
3. `stylebro` with the new subcommands.
4. No change.

Package: keep StyleBro.Migrate; **StyleBro.Tool** (owner's pick: named after what it is; existing installs need
uninstall + install); StyleBro.

Timing: after trial 31's PR; **now, in parallel** (owner's pick: whichever PR merges second resolves the conflicts);
some time before 1.0.

## Decision

`bro` with subcommands, package StyleBro.Tool, now. The project keeps its name (src/StyleBro.Migrate, assembly
StyleBro.Migrate.dll) to keep the change to the command and the docs. File markers written into users' files (`#
BEGIN stylebro-migrate`, `(stylebro-migrate init: ...)`) stay as they are, so existing blocks are still recognized.
The old StyleBro.Migrate package on nuget.org can be deprecated there (owner's step) once StyleBro.Tool is published.
