# Naming rules respect an IDE1006 suppression (2026-10-14)

## Question

MoreLINQ's `Lookup.cs` is copied from dotnet/runtime and starts with `#pragma warning disable IDE1006` to keep the
runtime's names; BRO1303 still renamed its `_count` to `count`. Should the naming rules (BRO1301-BRO1314) leave names
where the repository has turned the SDK's naming rule (IDE1006) off for that code?

## Choices

1. **Respect the suppression**: where IDE1006 is disabled by `#pragma warning disable IDE1006` (up to its restore) or by
   `[SuppressMessage(..., "IDE1006...")]` on the symbol, a containing member or type (or a global one targeting them),
   the naming rules don't report or rename. Tuple elements (BRO1311) and namespaces too.
2. Ignore it: BRO rules are suppressed by their own ids only.

## Decision

**Choice 1** (owner's choice 2026-10-14). The analyzer checks it last, so the fix (which only follows the analyzer's
findings) never renames there.
