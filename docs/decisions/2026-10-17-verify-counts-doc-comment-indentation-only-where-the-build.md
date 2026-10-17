# Verify counts doc comment indentation only where the build reports it (2026-10-17)

## Question

Since trial 27, `format` rewrites the indentation of documentation comment lines the whitespace pass leaves and the build's
IDE0055 reports (docs/decisions/2026-10-17-doc-comment-indentation-ide0055-reports-and-dotnet-format.md), and verify
counted them as IDE0055 findings, saying "the build reports it". Trial 28 found the build doesn't when the project
generates documentation (GenerateDocumentationFile): probed with SDK 10.0.402 and EnforceCodeStyleInBuild, a comment
indented with tabs under spaces was reported on no line, one with spaces under tabs on two lines of three (the closing
tag's line not). Verify was then not clean where the build was. What should verify count?

## Choices

1. **Count them only when the build would**: format keeps rewriting those lines, verify counts them only where the build
   reports them, as closely as is cheap; if that isn't predictable, only in projects without documentation generation.
2. Keep counting them everywhere (verify stricter than the build).
3. Stop counting them (verify laxer than the build where documentation isn't generated).

## Decision

**Choice 1** (owner's choice 2026-10-17: "count them only when the build would"). Which lines the build reports with
documentation generated didn't follow a rule cheap to mirror (the compiler parses the comments then, and IDE0055 sees
some lines and not others), so verify counts the lines only in the files of projects whose GenerateDocumentationFile
isn't `true`, where the build reports every one of them. The finding's text says so: "the build's IDE0055 reports it
where documentation isn't generated".
