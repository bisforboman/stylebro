# Migrate follows the code for formatter options StyleCop doesn't check (2026-10-18)

## Question

Trial 32: ControlzEx is StyleCop-clean, but after `bro migrate --write`, `bro format` re-indented its 7 `case X:` blocks,
whose `{` sits at the label's column. `init` counts that convention (`csharp_indent_case_contents_when_block`); `migrate`
writes only what the StyleCop setup says, and no StyleCop rule checks it. Should migrate also follow the code for
formatter options that no StyleCop rule covers?

## Choices

1. **Migrate detects it** (owner's pick): migrate counts the formatter conventions init counts but StyleCop never checked,
   and writes the code's form, so a StyleCop-clean repository stays unchanged.
2. Only case blocks.
3. Leave as is: the SDK's whitespace formatting applies, documented in docs/migrating.md.

## Decision

Migrate counts `case X:` block indentation like `init` and writes `csharp_indent_case_contents_when_block = false` when
the code clearly writes the blocks at the label's level (not under CSharpier); mixed code keeps the SDK's default and the
report says what `format` changes. The other formatter options `init` counts are StyleCop's already: brace and `else`
placement (SA1500), statements on one line (SA1107, where migrate writes `csharp_preserve_single_line_statements = true`)
and spacing (the SA10xx rules; when they are off, migrate already follows the code).
