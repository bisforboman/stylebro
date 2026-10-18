### Fixed

- **`bro --json check`** (and `--json` before `migrate` or `preview`) runs that command; it used to read the word as
  the folder to migrate. `bro preview --help` shows the help.
- **`bro check --diff`** says a preview and a check don't go together, instead of failing halfway through the preview.
- **`bro migrate init`** (or another command's name) migrates the folder of that name.
- **Temporary copies:** a later `bro` command deletes only the preview and check copies it made itself, and never one
  kept with `bro preview ... --keep`; folders with similar names that other programs made stay.
- **`bro init --write`** no longer writes the preset version into a `Directory.Build.props` that stops the import of the
  one above it (`<Project />`) when nothing under it references StyleBro.Analyzers.
- **`bro init`** no longer turns BRO1606 off because constructor summaries are in their own words: BRO1606 accepts those.
