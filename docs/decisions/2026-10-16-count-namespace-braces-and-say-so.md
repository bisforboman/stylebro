# Count namespace braces and say so (2026-10-16)

## Question

Trial 25 (MimeKit): the code writes `namespace X {` in 670 files, but the repository sets
`csharp_new_line_before_open_brace = methods,types`. Roslyn has no value for namespaces alone (`types` covers them), so
IDE0055 moved 614 namespace braces on the first `stylebro-migrate format`. What should `init` do?

## Choices

1. Count namespace braces and say so: when they disagree with the key in effect, the report says how many files would
   change and how to settle it, and IDE0055 stays off until the team decides.
2. Leave it to IDE0055 (the key is the team's; the braces move).
3. Change the key to match the namespaces (it would move the types' braces instead).

## Decision

Owner's choice 2026-10-16: 1, "Count namespace braces and say so". `init` counts one block namespace per file (its `{` on its
own line or at the end of the line). When most counted files disagree with the key in effect (the team's, init's own, or
the preset's `all`), the block gets a comment and `dotnet_diagnostic.IDE0055.severity = none`, and the report says "N
files will change", with the two ways out: set the key accordingly, or accept the change with
`dotnet_diagnostic.IDE0055.severity = warning` below the block. When only a minority disagrees, or the team sets IDE0055
itself (its severity stays, 2026-10-14), the report only says so.

The task named "IDE0055 at suggestion", but `stylebro-migrate format` runs its whitespace pass at any severity but
`none` (2026-10-14: "it does whatever IDE0055's severity says"), so `suggestion` would still have moved the braces; `none`
is what keeps `format` away from whitespace until the team decides.
