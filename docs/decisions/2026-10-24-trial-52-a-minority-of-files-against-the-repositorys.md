# Trial 52: a minority of files against the repository's end_of_line (2026-10-24)

## Question

error-or's `.editorconfig` says `[*] end_of_line = crlf`; 36 of its 48 C# files are CRLF and 12 LF as committed (no
.gitattributes, git converts nothing). StyleCop never enforced it. The migration (and init) turns IDE0055 on, so
`bro format` rewrote all 12 LF files to CRLF: 12 whole-file diffs, about 1,000 of the 1,800 changed lines, and nothing
said so. How should init and migrate handle a repository's own end_of_line when a minority of files disagree with it?

## Choices

- **Warn only** (recommended): keep the repository's end_of_line; init and migrate say which files 'format' converts
  and how to keep that out of the formatting changes. Follows "Keep endings out of it, warn" (2026-10-12).
- **Keep their endings**: also write per-file sections (`[{a.cs,b.cs}] end_of_line = lf`), as init does for files with
  multi-line string literals.
- **Lower IDE0055**: set it to suggestion where files disagree with the key, so 'format' formats no whitespace there.

## Decision

Warn only (owner, 2026-10-24).
