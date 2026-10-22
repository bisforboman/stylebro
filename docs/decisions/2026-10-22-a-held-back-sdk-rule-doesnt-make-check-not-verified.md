# A held-back SDK rule doesn't make check Not verified (2026-10-22)

## Question

Some .NET SDKs' `dotnet format` (10.0.1xx) ignores StyleBro's suppressors for IDE rules, so `bro format` and `bro check`
hold back IDE0049 (and the other fixes the multi-target guard covers) in every repository with a multi-targeted project
or a file another project compiles with an older C# (decision of 2026-10-20). Since the gap list of 2026-10-20, that
held-back rule was a `Not checked:` gap, so on those SDKs every such repository ended "Not verified" with exit code 3,
however clean it was. Should a held-back SDK rule still decide the verdict?

## Choices

1. **Note it, don't fail** (owner's pick): the held-back rules are still listed under `Not checked:` (and in `--json`'s
   `notChecked`), but on their own they leave the verdict and the exit code alone. StyleBro's own gaps (projects or
   frameworks not loaded, configurations skipped, files excluded, fixes that can't run such as xunit.analyzers without
   Fix All) still make it "Not verified" with exit code 3.
2. Keep it a gap: "Not verified" and exit code 3 until a newer SDK is installed.
3. Drop the line: say "Held back: ..." only.

## Decision

Note it, don't fail: `bro format`, `bro check` (and `--json`) and `bro preview` list the held-back rules with a
`Not checked:` line, and end clean (exit code 0) when nothing else is left.
