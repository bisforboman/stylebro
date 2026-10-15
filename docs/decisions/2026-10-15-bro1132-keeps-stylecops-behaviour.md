# BRO1132 keeps StyleCop's behaviour (2026-10-15)

## Question

BRO1132 (SA1108) moves a trailing comment after a condition into the block. That breaks line-based suppressions of
other tools, such as akka.net's `catch (X) when (...) // slopwatch-ignore: SW003`: once moved, the comment no longer
sits on the line the tool checks. Should BRO1132 leave such tool-directive comments where they are?

## Choices

1. **Keep StyleCop's behaviour**: every embedded comment moves, as SA1108 reports it; a repository whose tools need the
   comment on the header's line turns BRO1132 off or accepts the move.
2. Skip tool-directive comments (a list of known prefixes, or a `stylebro_*` option naming them).

## Decision

**Choice 1** (owner's choice 2026-10-15). The rule page says how to handle suppression comments.
