### Fixed

- **`bro check --build`** no longer says "Not clean" in a shallow clone of a repository that versions from git history
  (Nerdbank.GitVersioning and the like): the copy of HEAD it compares with now has the repository's history, so its builds
  fail the same way and those errors aren't counted as new.
