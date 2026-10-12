# Summary wording: init detection, BRO1606 on own text, BRO1604 on bool clauses (2026-10-12)

## Question

[Issue #132](https://github.com/bisforboman/stylebro/issues/132), from a trial on 0.5.0-alpha.1: the repository writes
terse summaries (`The samples, ...`, `Whether the decoder ...`, constructors `Opens a file or URL ...`), and `format`
rewrote about 100 public summaries through BRO1604 and BRO1606 (101 and 12 findings; about 0 of 113 summaries were in the
StyleCop form). BRO1606 put the standard sentence in front of a summary that already said what the constructor does, and
BRO1604 turned a bool's clause into `Gets or sets the tokenizer predicted a multi-word token here`.

## Choices

1. `init` counts how property and constructor summaries begin, like its other conventions (at least 3 places, 75%), and
   turns BRO1604/BRO1606 off when few or a mix follow the StyleCop form.
2. BRO1606 on a constructor summary with text of its own: (a) as before, the standard sentence in front; (b) not
   reported, only empty, filler and near-miss summaries get the standard sentence (quieter than SA1642).
3. BRO1604 on a bool summary that starts with an article: (a) as before, `Gets or sets the tokenizer predicted ...`;
   (b) not reported; (c) `Gets or sets a value indicating whether the ...` (breaks real noun phrases: `... whether the
   open state`).
4. `init --diff`: each rule's share of the changed lines (needs a format run per rule).

## Decision

Owner, 2026-10-12: 1 yes; 2b; 3b; 4 no (left out). The StyleCop form counted for properties is a summary starting with
`Gets`, `Sets` or `Initializes`; for constructors `Initializes a new instance`, `Initializes static members` or
`Prevents a default instance`. A summary that starts with an element isn't counted. BRO1607 (finalizers) is unchanged.
