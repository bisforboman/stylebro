# Trial 53: a minority of files against the repository's charset or final newline (2026-10-24)

## Question

BaGet's `.editorconfig` says `[*] charset = utf-8`; 16 of the 248 C# files init counts start with a BOM (committed that way,
nothing enforced it). Init turns IDE0055 on, so `bro format` stripped the 16 BOMs: whole-file changes nothing announced.
Trial 52's "Warn only" covered this for end_of_line only. How should init and migrate handle charset and
insert_final_newline when a minority of files disagree with the repository's own key?

## Choices

- **Warn only** (recommended): as for end_of_line; keep the repository's key, and init and migrate say how many files
  'format' changes (BOM removed or added, last line break added or removed) and how to keep that out of the formatting
  changes. For charset and insert_final_newline.
- **Keep each file as is**: write per-file sections (or 'unset') so 'format' leaves those files alone.
- **Leave it silent**: the repository's setting decides; the preview already lists "CHARSET 16 files".

## Decision

Warn only (owner, 2026-10-24).
