# Run CSharpier after format (2026-10-16)

## Question

Trial 24 (Dawnshard): after `stylebro-migrate format`, 21 files failed the CI's `dotnet csharpier check .` (BRO1149's
`)` placement, BRO1150, BRO1141 past the line width, BRO1138, BRO1001's blank lines). `format` only said "CSharpier
formats this repository: run it now", and `--verify-no-changes` said clean. What should `format` do in a repository that
formats with CSharpier?

## Choices

1. Run the repository's own CSharpier on the files `format` changed, at the end of each run, and have
   `--verify-no-changes` check both: StyleBro clean and `csharpier check` on the C# files CSharpier formats.
2. Keep the note and leave CSharpier to the user (and their CI).
3. Make every fix lay out its code exactly as CSharpier would (not reachable: CSharpier's layout depends on the whole
   statement and its line width).

## Decision

**Choice 1: Run CSharpier after format** (owner's choice, 2026-10-16). Where CSharpier is a dotnet tool
(`dotnet-tools.json`, restored first) or comes with the CSharpier.MsBuild package (its own copy of CSharpier), `format`
runs `csharpier format` on the C# files each run changed, in the folders CSharpier formats, so one command leaves a
CSharpier-clean tree. `--verify-no-changes` runs `csharpier check` on those folders' C# files too. Where CSharpier puts
back what a fix changed (IDE0036's `static file` against CSharpier's `file static`), no run can settle it: `format` and
`--verify-no-changes` both say so and name the files. `init` turns IDE0036 off in CSharpier mode for that reason.
