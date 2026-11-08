### New

- **Fewer merge conflicts in `stylebro.baseline`.** When `bro format` or `bro baseline` writes a baseline in a git
  repository, it adds `stylebro.baseline merge=union` to the `.gitattributes` at the repository's root (unless a line
  there already names the baseline), so a local merge or rebase keeps both branches' entries instead of stopping at a
  conflict. ([Merging branches](https://bisforboman.github.io/stylebro/baseline/#merging-branches))

### Changed

- **A baseline entry on two lines** now allows the larger of their counts, not their sum, so a merged baseline never
  allows violations that neither branch had.
