# Init keeps the repository's own IDE0055 severity (2026-10-14)

## Question

MoreLINQ sets `dotnet_diagnostic.IDE0055.severity = suggestion`. `init`'s block wrote `warning`, which won (the block
comes later), while the report said the spacing was decided by "your .editorconfig sets IDE0055 (suggestion)". Should
`init` keep the repository's own IDE0055 severity?

## Choices

- Keep the repository's severity: the block leaves IDE0055 out, and the report says so. `stylebro-migrate format` still
  runs its whitespace pass (it runs whatever IDE0055's severity is).
- Keep writing `warning` and correct the report.

## Decision

Owner's choice 2026-10-14: keep the repository's severity, and say so correctly ("sets IDE0055 (suggestion): kept, the
block doesn't set it"). With CSharpier or column alignment `init` still writes its own `none` line (those decide the
layout).
