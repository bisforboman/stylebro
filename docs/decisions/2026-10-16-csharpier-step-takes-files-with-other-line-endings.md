# CSharpier step takes files with other line endings (2026-10-16)

## Question

Trial 26 (serde-like repository with CSharpier and `end_of_line = lf`): one C# file had CRLF line endings. `format` runs
no whitespace pass where CSharpier formats and ran CSharpier only on the files its fixes changed, so it said clean (exit
0), `dotnet csharpier check .` failed and `format --verify-no-changes` exited 2. Should `format` fix such files?

## Choices

1. The CSharpier step also takes every C# file whose line endings differ from `end_of_line`, so `dotnet csharpier check`
   passes afterwards.
2. Leave them, and let verify say what to run.

## Decision

**Choice 1** (owner's choice, 2026-10-16). Only where `end_of_line` is `lf` or `crlf` for the file (not `unset`), and only
C# files in CSharpier's folders. Also: `--verify-no-changes` counts CSharpier's findings in Directory.Build.props and
Directory.Packages.props (the MSBuild files `init` edits; CSharpier 1.x checks them), not only in C# files.
