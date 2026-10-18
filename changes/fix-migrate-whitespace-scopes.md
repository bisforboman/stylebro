### Fixed

- **bro migrate --write** judges `end_of_line`, `charset` and `insert_final_newline` against the .editorconfig that
  actually decides them for each file, like `init` does: a nested .editorconfig (e.g. `Source/.editorconfig`) that sets
  them gets its own `stylebro-whitespace` block, and files under it no longer count for the root's settings. Before, the
  migration could promise that `format` leaves line endings or final newlines alone while the nearer file made it
  rewrite them.
- **bro init** and **--write** remove a `stylebro-whitespace` block from a nested .editorconfig that no
  longer decides those settings, so running them again stays clean.
