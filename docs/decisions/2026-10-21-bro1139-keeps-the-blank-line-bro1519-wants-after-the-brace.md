# BRO1139 keeps the blank line BRO1519 wants after the brace it removes (2026-10-21)

## Question

Trial 41, ClosedXML: `else { if (x) y; }` followed by `return this;` came out differently in `bro preview` and the real
`bro format` run, depending on which fix `dotnet format` applied first. BRO1519 first adds a blank line after the `}`, and
BRO1139's `else if` join keeps it; BRO1139 first leaves no `}`, so BRO1519 has nothing to report. Which result should both
orders give?

## Choices

1. **Keep the blank line** (owner's pick): the join adds the blank line BRO1519 wanted after the removed `}`, as other
   shape-changing fixes add BRO1519's line themselves.
2. No blank line: BRO1519 leaves a `}` that BRO1139 removes alone.

## Decision

Keep the blank line. `ElseIfs.GetChanges` adds it when `BlankLineRuns.JudgesGapAfter` says BRO1519 judges the gap; when the
join also adds braces, the brace wrap brings the blank line already.
