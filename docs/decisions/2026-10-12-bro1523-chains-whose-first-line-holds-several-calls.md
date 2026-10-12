# BRO1523: chains whose first line holds several calls (2026-10-12)

## Question

[Issue #135](https://github.com/bisforboman/stylebro/issues/135), from a trial on 0.5.0-alpha.1: in
`sb.Append(id).Append('\t').Append(text).Append('\t')` + newline + `.Append(text.Length).Append('\t')` + newline + ...,
BRO1523 split the calls below the first line onto their own lines and left the four calls on the first line, because the
first line is free ([2026-10-04](2026-10-04-call-chain-layout-bro1523-roslynator-rcs0054.md), after Roslynator's
RCS0054). The result is still a mixed layout, and the message ("like the chain's other calls") didn't describe the code.
In the survey of the reference repositories, 58 of 20,959 split chains had two or more calls on the first line (mostly
FluentValidation's `RuleFor(x => x.Name).NotNull()` + newline + `.WithMessage(...)`).

## Choices

- **A.** Keep the rule as it is and only fix the message.
- **B.** Leave chains that fill lines alone: a chain whose first line already holds two or more calls isn't reported.
- **C.** Split the first line too, down to the receiver and one call (`sb.Append(id)` + newline + `.Append('\t')` ...).

## Decision

B (owner, 2026-10-12). The receiver's own call counts (`RuleFor(x).NotNull()` holds two). The message no longer says
"like the chain's other calls": "Start '.X' on its own line: in a split chain, each call after the first line starts
one".
