# init warns when git ignores the .editorconfig it writes (2026-10-22)

## Question

Trial 43, NSwag: its .gitignore has a line `.editorconfig`, so the settings `bro init --write` writes there are never
committed (teammates and CI get only the preset), and `bro preview`'s patch left the file out (applying the patch gives a
different result than the preview showed). What should init and preview do when git ignores the .editorconfig they write?

## Choices

1. **Warn, and the patch includes it** (owner's pick): init (and migrate) say the file is ignored, by which .gitignore line,
   and how to commit it (`git add -f` or remove the line); the preview's patch includes the ignored .editorconfig files, so
   it matches what the preview showed. Nothing else changes.
2. Stop, like a StyleCop repository: `init --write` refuses until the .gitignore line is gone.
3. Write the settings somewhere git keeps (a committed .globalconfig); `dotnet format`'s style pass ignores IDE severities
   there.

## Decision

Warn, and the patch includes it: `InitCommand.IgnoredLine` (after each written file, init and migrate) and
`PreviewCommand.AddIgnoredSettings` (before the preview's baseline and settings commits; bin/obj left out).
