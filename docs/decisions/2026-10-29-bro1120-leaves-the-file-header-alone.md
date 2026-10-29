# BRO1120 leaves the file header alone (2026-10-29)

## Question

Trial 60, apache/logging-log4net: BRO1120 (empty comments) removed the empty first and last `//` lines of the Apache
license header in 325 of 402 files. The header sits inside a leading `#region Apache License`. StyleCop's SA1120 reports
the same; BRO1120 left the header alone only when `file_header_template` was set, and then only a header at the very top.
What should BRO1120 do with the comment block a file starts with?

## Choices

- **Leave file headers alone** (recommended): BRO1120 never touches the comment group a file starts with, also when it
  sits right inside a leading `#region`. License text stays as written; a documented difference from StyleCop.
- Keep StyleCop's behaviour: the header's empty lines go.
- Let init decide: init counts headers with empty `//` lines and turns BRO1120 off when most have them.

## Decision

Leave file headers alone (owner, 2026-10-29).
