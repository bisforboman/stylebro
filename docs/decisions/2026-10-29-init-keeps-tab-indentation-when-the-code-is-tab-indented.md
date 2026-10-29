# init keeps tab indentation when the code is tab-indented (2026-10-29)

## Question

Trial 62, jbevain/mono.linq.expressions has no .editorconfig and indents every line with tabs (4,771 lines, 0 with spaces).
init wrote nothing about indentation, so the preset's `indent_style = space` applied: BRO1107 inserted new lines indented
with spaces into tab-indented files (mixed indentation; IDE0055 was off there, else it would have converted every tab).
What should init do?

## Choices

- **Detect it, keep tabs** (recommended): init counts C# files indented with tabs or spaces and writes
  `indent_style = tab` (kept, like `end_of_line`) when the code uses tabs; the usual thresholds, mixed leaves the default.
  New lines from fixes then get tabs.
- Keep as is: the preset's spaces stay; a tab repository has to set `indent_style` itself.

## Decision

Detect it, keep tabs (owner, 2026-10-29). Counted per file (most of its indented lines start with a tab, or with two
spaces or more); the repository's own `indent_style` wins, as for every convention.
