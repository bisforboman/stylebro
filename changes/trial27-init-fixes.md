### Changed

- **`stylebro-migrate init`** recognizes a vendored folder by a license file of its own whose copyright holder isn't the
  root license's (a fork copied in with its `LICENSE`): it marks it `generated_code = true` and leaves it out of the count.
- **`stylebro-migrate --write`** follows your `.editorconfig`'s naming rule for private static fields over SA1309 when no
  such field in the code breaks it (it's the rule left after the swap).

### Fixed

- **`stylebro-migrate init`** run from a subfolder works on the repository's root also when the root has only a
  solution that lists the folder's projects, when a project in the folder references one outside it, and when the folder
  has no project at all; before, it wrote a separate `.editorconfig` and `Directory.Build.props` there.
- **`stylebro-migrate init`** counts `using Helper;` (a type in an enclosing namespace) and `using Text;` inside a
  `System.*` namespace as relative usings, so BRO1126 no longer rewrites code that mostly uses them.
- **`stylebro-migrate init --style microsoft`** says that private static fields without a prefix get the style's `s_`
  (it said "BRO1307 is off" and `format` then renamed them), and drops lines of an earlier `init` that the style repeats.
  The summary names both static field keys when it writes both.
- **`stylebro-migrate init`** and **`--write`** no longer stop on a C# or project file another program has locked.
