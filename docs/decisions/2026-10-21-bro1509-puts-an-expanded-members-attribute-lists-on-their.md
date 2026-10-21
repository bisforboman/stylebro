# BRO1509 puts an expanded member's attribute lists on their own lines (2026-10-21)

## Question

Trial 41, a spot check of BRO1102's `[A][B]` with the SDK formatter: when BRO1509 expands a one-line member with attributes
on its line (`[Obsolete][Description("a")] public void M() { }`, BRO1102's output; `[Obsolete] public void M() { }` alone
does it too), the SDK formatter wants each attribute list of a member that spans several lines on its own line (probed with
`dotnet format whitespace`: types, methods, constructors, operators, properties, indexers, events, accessors; not local
functions or parameters; a one-line member keeps them). `dotnet format` runs its whitespace pass before the analyzers'
fixes, so the lists moved only on the next run: `bro format` needed two changing runs, and a second plain `dotnet format`
changed the file again. How should BRO1509 handle it?

## Choices

1. **Own lines** (owner's pick): the expansion also breaks the line after each of the member's attribute lists, at the
   member's indentation, as the formatter would. One run.
2. Don't report a one-line member with attributes on its line.
3. Leave it: two runs, documented.

## Decision

Own lines. `SingleLineBlocks.AddAttributeLines` adds the breaks for the expanded node's owner when it is a member
declaration; a comment in such a gap skips the node.
