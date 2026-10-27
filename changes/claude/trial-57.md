### Fixed

- **BRO1404** no longer adds an access modifier to a declaration whose modifier sits in an `#if` branch the build skips
  (`#if !X` / `file` / `#endif` / `static class`): where the branch is on, that gave `file internal` (CS9052).
- `bro check` and `bro format` no longer say "Clean" when a project's design-time build fails while loading (for example
  Nerdbank.GitVersioning in a shallow clone): such a project loads without its build settings, so StyleBro's rules didn't
  run there. It's named as `Not checked`, with MSBuild's error, and the verdict is "Not verified".
- `bro preview` gives its copy the repository's git history, remotes and shallow boundary, so version tools such as
  Nerdbank.GitVersioning see what they see in the repository (a negative version height broke the load there, and the
  preview showed fewer changes than `bro format` made).
- `bro check --build` no longer says "Not clean" for errors in a project the untouched code's build didn't compile (a
  project it needs fails there too): they have nothing to compare with, so it says "Not verified" and names the project.
- `bro check --build` prints its "Not verified" verdict after the builds, and says what they checked, instead of before
  them with a hint to run `bro check --build`.
