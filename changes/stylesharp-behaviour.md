### New

- `stylebro_summary_wording = stylesharp` makes BRO1604-BRO1607 follow StyleSharp's summary rules: a `protected` setter
  counts as restricted, a getter counts whatever its modifier, `Gets or sets` on a read-only property is fine, and a
  constructor summary only needs the standard words before the type.
- `stylebro_member_order` takes a `record` kind (records after classes, as StyleSharp orders them) and kinds joined by
  `|` that share a place (`conversion|operator|method`).
- `stylebro_trailing_comma_switch_expressions = false` leaves switch expressions to you (StyleCop 1.1 and StyleSharp
  don't ask for the comma there).

### Changed

- `bro` on a StyleSharp repository writes StyleSharp's own meaning where its rule differs from the StyleCop
  rule with the same number: the settings above, StyleSharp's list of words a name may start with (`id`, `ok`, `db`,
  ...), one-line empty blocks, no period check in `<param>`, `<typeparam>` and `<exception>`, and no namespace naming.
  StyleSharp-clean code no longer gets changes StyleSharp then reports (`SST1624`, `SST1201`).
- `bro` on a StyleCop 1.1 repository keeps BRO1401 on and only leaves switch expressions alone, instead of
  turning the rule off when the code had a switch expression without the comma.

### Fixed

- StyleSharp's SST1108 (files of commented-out code) no longer counts as StyleCop's SA1108.
- The migration report names StyleSharp where StyleSharp runs and says "everywhere except" when that list is the shorter
  one, and no longer prints a Sonar section for Sonar ids that are all turned off with no Sonar package.
