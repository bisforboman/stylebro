# The old engine leaves out C# projects whose load reaches Visual Basic (2026-10-27)

## Question

The fixture matrix's visual-basic shape found that where C# code references a Visual Basic project, IDE0036's Visual Basic
fixer also ran on the C# diagnostics and deleted a C# member. The host (the default engine) is fixed in the same PR: it calls
only fixers exported for the diagnostics' language. The old engine (`--engine dotnet-format`, also the host's fallback when
its workspace doesn't load) has the same bug inside `dotnet format`, depending on its fix order. What should it do?

## Choices

- Fall back off it: under the old engine, C# projects whose load reaches a Visual Basic project are left out of the runs and
  named as "Not checked" (Not verified, exit 3); the default engine checks them.
- Leave it and document it: the old engine goes away after the next release.
- Drop the ids that have both C# and Visual Basic fixers (IDE0036 at least) from such runs, with a note.

## Decision

"Fall back off it". `FormatCommand.ReachingVisualBasic` (project references, transitively through the C# projects) and
`VisualBasicEngineGap`; the fixture expects exit 3 with that line under `--engine dotnet-format` and the host's clean result
by default (`EnginesDiffer`).
