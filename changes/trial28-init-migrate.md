### Changed

- **`stylebro-migrate init`** and **`--write`** count only C# files a project compiles: a file below no project (a Unity
  `Assets/` folder) no longer decides a convention, unless a project links files from elsewhere or a shared project
  could include it.
- **`stylebro-migrate --write`** follows your `.editorconfig`'s naming rule for private `static readonly` fields (one
  requiring just `static` covers them) when no such field breaks it, instead of pinning them to PascalCase.

### Fixed

- **`stylebro-migrate init`** and **`--write`** treat `end_of_line = unset` like no setting: when the C# files agree,
  they write their ending, so `format` no longer writes your OS's ending into the lines it changes while the build's
  IDE0055 reports them.
- **`stylebro-migrate --write`** no longer crashes halfway on a file another program has open: it names the file and
  writes nothing, so the migration is never half done.
- **`stylebro-migrate init`** says which files it couldn't read (open in another program) instead of quietly counting
  without them; code whose license file can't be read is left out and not marked as vendored.
- **`stylebro-migrate init`** no longer takes a license template's placeholder (Apache's
  `Copyright [yyyy] [name of copyright owner]`) for a copyright holder, which marked your own folders as vendored code.
- **`stylebro-migrate --write`** no longer warns that a project copies an `.editorconfig` when it only shows it in the
  solution (`<None Include="..\.editorconfig">`); the SDK's item types copy at most into the output folder.
- **`stylebro-migrate init`** leaves `///` comments that document nothing out of the one-line summary count, and says
  how many private fields follow the naming rule that decides their style.
