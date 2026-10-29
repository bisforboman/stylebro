# init turns BRO1120 off when the code frames its comments with empty comment lines (2026-10-29)

## Question

Trial 62, jbevain/mono.linq.expressions (Mono style): 4 of its 11 multi-line `//` comments inside code are framed by an
empty `//` line above and below (`//` / `// text` / `//`); BRO1120 (SA1120 parity) removed those 8 empty lines (trial 60
exempted only file headers). Should init get a convention for it?

## Choices

- **Detect it, BRO1120 off** (recommended): init counts comment groups framed by empty `//` lines against the others;
  when framing is the code's convention or mixed (the usual thresholds), BRO1120 goes off, like BRO1503 and BRO1308. Here
  4 of 11: mixed, off.
- Keep as is (SA1120 parity): BRO1120 keeps removing empty `//` lines at the ends of a comment, as StyleCop reports them.

## Decision

Detect it, BRO1120 off (owner, 2026-10-29). Counted: the comment groups BRO1120 checks that have text (not the file
header, not an empty comment a pair of braces holds, not a group of empty comments only), framed when an empty comment
starts or ends them.
