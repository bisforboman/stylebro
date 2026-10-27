# A project MSBuild fails to load is named and the verdict is Not verified (2026-10-27)

## Question

Trial 57, DapperAOT: when MSBuild fails while loading a project (Nerdbank.GitVersioning's "build ('-2') must be a
non-negative value" in a depth-1 clone and in the preview's copy: `versionHeightOffset` -4), `dotnet format` still analyzes
the project, but without its compiler settings, so StyleBro's rules stay off there. In a shallow clone `bro format` then
`bro check` said "Clean." with 22 files of BRO findings left (the full clone formats 186 files, the shallow one 164). bro
only printed its "warnings loading the workspace" note. What should happen?

## Choices

- **Name it, Not verified**: a `Not checked: <project> (<framework>): MSBuild failed loading it: <message>` line on both
  engines; the verdict becomes "Not verified" (exit 3), not Clean.
- Stop with an error (exit 1) and check nothing.
- Leave as is (only the load-warning note).

## Decision

"Name it, Not verified" (owner, 2026-10-27).
