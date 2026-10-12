### Changed

- **stylebro-migrate**: StyleCop settings in a repository where no project references StyleCop.Analyzers any more are
  leftovers. `init` now proceeds there (it notes the lines and follows the code), and `--write` explains that StyleCop
  isn't installed and points to `init` instead of counting every rule the leftovers don't mention as on.

### Fixed

- **stylebro-migrate**: a StyleCop reference under an MSBuild `Condition` (for example only outside test projects) is
  now evaluated with MSBuild, so the settings only go where StyleCop really ran; a folder with its own `root = true`
  .editorconfig where StyleCop didn't run gets no settings.
- **stylebro-migrate**: the package swap puts StyleBro.Analyzers next to a conditional StyleCop reference, under the
  same condition, instead of in a new unconditional ItemGroup.
- **stylebro-migrate**: the line-ending note says how many files `format` converts when the files are mostly one
  ending, instead of promising it leaves endings alone.
- **stylebro-migrate**: the private-field note judges SA1309 where the code is (a sub-folder .editorconfig that turns it
  off, with all the code under it, now counts).
- **stylebro-migrate**: suppressions are no longer carried over into generated code (also checked-in source generator
  output), nor to IDE0055, which formatting ignores (the report counts them); lower-case `sa1234` ids are read
  everywhere.
- **stylebro-migrate**: `net4.7` and `net47` share one format run; the "several solutions" message names every choice;
  "2 summaries" instead of "2 summarys".
