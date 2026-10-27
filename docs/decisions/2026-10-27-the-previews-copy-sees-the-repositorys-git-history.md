# The preview's copy sees the repository's git history (2026-10-27)

## Question

Trial 57, DapperAOT: the preview's copy got a new git repository with two commits, so Nerdbank.GitVersioning computed
another version height than in the repository; with `versionHeightOffset` -4 it was negative and the load failed (the
preview listed 164 files, format then changed 186). Should the preview's copy see the repository's history?

## Choices

- **Real history**: the copy's repository borrows the original's objects (git alternates) and starts from its HEAD commit,
  so version tools see the same history plus the preview's own commits; a shallow original stays shallow.
- Only report it (the load-failure gap of the decision above names it in the preview).
- Leave as is.

## Decision

"Real history" (owner, 2026-10-27).
