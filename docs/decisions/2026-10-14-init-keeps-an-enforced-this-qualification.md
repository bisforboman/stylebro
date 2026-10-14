# init keeps an enforced this qualification (2026-10-14)

## Question

Microsoft.IO.RecyclableMemoryStream's `.editorconfig` requires `this.` (`dotnet_style_qualification_for_field = true:warning`
and the same for properties, methods and events). `init`'s block wrote `dotnet_diagnostic.IDE0009.severity = none`
(SA1101 is off in the preset), which turned the repository's own rule off. What should `init` do when the repository
enforces `this.`?

## Choices

1. **Leave IDE0009 out of the block** when the repository enforces `this.`: a `dotnet_style_qualification_for_*` key set
   to `true` with a severity that isn't `silent` or `none`, or IDE0009 at `suggestion` or above (judged like init's other
   "only enforced" checks). The repository's rule stays.
2. Keep writing `none` (the preset's choice wins).

## Decision

**Choice 1** (owner's choice 2026-10-14).
