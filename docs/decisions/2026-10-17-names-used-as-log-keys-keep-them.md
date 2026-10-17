# Names used as log keys keep them (2026-10-17)

## Question

Trial 28 (ZLogger): an interpolated string handler whose `AppendFormatted` takes the hole's text as a
`[CallerArgumentExpression]` argument logs `$"user {UserId}"` with the key `UserId`. BRO1301 renamed the local to `userId`,
which compiled, but the structured log's key changed (the repro printed `userId=42` instead of `UserId=42`). Should the
renames keep such names?

## Choices

1. Keep such names: a local, parameter, field, property or tuple element used in a hole whose handler's AppendFormatted
   has a `[CallerArgumentExpression]` parameter keeps its name, as a kept finding with a clear reason (format lists it and
   baselines it).
2. Rename anyway: the key is the code's text, renaming changes it like any other refactoring would.
3. Don't report such names at all.

## Decision

**Choice 1: Keep such names** (owner's choice, 2026-10-17). Every renamer checks each use: BRO1301-BRO1309 (the shared
renamer) and BRO1311 (tuple elements). The kept finding says `the name is a log key ...` and where it is used, with the
handler's name (`Program.cs(12) in ZLoggerInterpolatedStringHandler`). Only interpolated strings converted to a handler
type are bound, once per hole.

This includes parameters. The earlier decision (#56) that BRO1302 keeps renaming a parameter whose name reaches run time
through `[CallerArgumentExpression]` (`ArgumentNullException.ThrowIfNull(key)`, an exception's ParamName) stands: that's
an error message. A name in a log key hole is data a log pipeline stores and queries by, the same run-time-data case as a
name in a string, so a parameter used there keeps its name too.
