### Fixed

- **`bro format`/`bro check`** no longer load the projects a solution lists but never builds (no build configuration
  includes them). They are left alone as before, but their load failures no longer appear as "Not checked" gaps.
