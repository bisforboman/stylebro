# init turns BRO1519 off when statements after blocks are mixed (2026-10-16)

## Question

`stylebro-migrate init` counts statements right after a statement block's `}` (with a blank line above, or right below
the `}`), but had no off line for BRO1519 when the code is mixed: it stayed on and added hundreds of blank lines in
trial 26 (Serde: 365 with a blank line, 543 without; NLog: 1,417 to 480). What should init do with mixed code?

## Choices

- Off when mixed, like every other mixed convention: BRO1519 = none unless at least 75 % of the places already have the
  blank line.
- Keep BRO1519 on (StyleCop's SA1513 default) and let `format` add the blank lines.

## Decision

Off when mixed (owner, 2026-10-16). The usual threshold applies: 75 % of at least 10 places (or 3 that all agree) with
the blank line keeps BRO1519 on; mostly without, or mixed, writes `dotnet_diagnostic.BRO1519.severity = none` with a
`# init:` comment saying how to turn it back on.
