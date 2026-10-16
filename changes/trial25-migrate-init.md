### Changed

- **stylebro-migrate init** counts namespace braces: when most files put a namespace's `{` where
  `csharp_new_line_before_open_brace` doesn't (Roslyn's `types` covers namespaces), it says how many files would change
  and turns IDE0055 off until you decide, so `format` doesn't move them.
- **stylebro-migrate init** reports two more conventions: statements after a block's `}` (BRO1519), and nested `if`s
  BRO1149 would merge; with a few nested `if`s in the code, BRO1149 is off.
- **stylebro-migrate** (`--write`) records `stylecop.json`'s settings in its block, so `stylecop.json` can go after the
  package swap; the report says so.
- **stylebro-migrate** (`--write`) turns StyleBro's rules and the built-in ones its block sets off by id in the folders
  without StyleCop, so the build there agrees with `format`, which leaves those folders alone.
- **stylebro-migrate** (`--write`) maps StyleSharp's SST1653 (short summaries on one line) to BRO1616 with
  `stylebro_summary_layout = single_line_when_fits`.
- **stylebro-migrate** says when a new `Directory.Build.props` hides one above the repository.

### Fixed

- **stylebro-migrate** (`--write`): a second run after removing StyleCop.Analyzers and `stylecop.json` no longer turns
  settings back to StyleCop's defaults (documentation of internal elements, using placement, file header company).
- **stylebro-migrate** (`--write`) writes `csharp_prefer_braces = true` while SA1503 is on, also when your
  `.editorconfig` says `when_multiline`: braceless one-line statements stay reported.
- **stylebro-migrate init**: an IDE rule's own severity (`dotnet_diagnostic.IDE0011.severity = warning`) now counts over
  the option's suffix (`csharp_prefer_braces = true:silent`), as in Roslyn; and when the block turns off a built-in rule
  you turned on (IDE0011, IDE0040, ...), the report says which StyleBro rules enforce its setting instead.
- **stylebro-migrate init**: doc comments with only `<param>`, `<typeparam>` and `<returns>` count for the doc element
  order when there are few others (BRO1619 reordered `<returns>` above `<param>` everywhere).
