### Changed

- **`--diff`** writes the full diff to `.git/stylebro-preview.patch` (outside a git repository: the temp folder) instead
  of an untracked file in the current folder; `--diff=file` still names another file.
- **`bro format`** says once what "Could not format '...fsproj'" and "Warnings were encountered while
  loading the workspace" mean, instead of repeating them in every run.

### Fixed

- **`bro format`** and **`baseline`** leave a folder CSharpier formats out of the whitespace pass when
  CSharpier only formats part of the repository (`IDE0055 = none` in that folder's section), so the two formatters no
  longer undo each other's changes forever (`[A] [B]` against `[A][B]`).
- **BRO1603-BRO1607, BRO1617-BRO1619** leave documentation comments with malformed XML alone: `IComparable<T>` written
  in prose no longer became `IComparable.<T>`.
- **BRO1603** removes trailing spaces after the period it adds, so the line doesn't end in `. `.
- **BRO1604/BRO1605** treat a private or internal getter like StyleCop does: `{ private get; set; }` is documented
  "Sets ...", not "Gets or sets ...".
- **BRO1606** accepts the standard constructor sentence with a spaced cref (`<see cref = "X" />`) instead of rewriting it.
- **`--diff`** no longer tells you to run `dotnet restore` for `packages.config` projects it left out on purpose.
