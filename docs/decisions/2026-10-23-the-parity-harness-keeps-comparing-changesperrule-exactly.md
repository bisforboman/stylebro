# The parity harness keeps comparing changesPerRule exactly (2026-10-23)

## Question

`scripts/format-speed/Compare-Format.ps1` compares the `--json` reports of both engines. In trial 47 the default engine alone
gave 104 and 105 BRO1505 changes on the same CSharpFunctionalExtensions code: `dotnet format` fixes the ids in an order
that changes from one process to the next, and another fix sometimes adds a blank line BRO1505 would have. The host's
order is fixed (105 every time). How should the harness treat that?

## Choices

1. **Note it, keep the exact comparison** (owner's pick): document the variance (docs/format-engine.md); when only the
   counts differ and the trees are the same, run again.
2. Tolerate count drift: with identical trees, let changesPerRule differ by a little and report a note.

## Decision

Note it, keep the exact comparison.
