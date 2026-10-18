### Fixed

- **init with CSharpier:** IDE0036 (modifier order) is off too: CSharpier orders modifiers itself, in another order
  (`file static class` and `static file class` went back and forth).
- **init, compact members:** when members have no blank lines between them and BRO1505 goes off, BRO1519 goes off too;
  it added a blank line after every method's `}` anyway.
- **init, one-line blocks:** method bodies and statement blocks with one short statement on one line
  (`int Next() { return ++count; }`, `if (x) { return; }`) are counted; when the code writes them that way, BRO1509 and
  BRO1508 go off (and `csharp_preserve_single_line_statements = true` keeps IDE0055 from splitting them).
- **init, doc element order:** doc comments with params or returns next to `<remarks>`, `<exception>` and the like are
  counted; when the code puts them in another order, BRO1619 goes off instead of reordering them.
- **init:** folders whose own `.editorconfig` turns every analyzer off or says `generated_code = true` no longer count
  towards the conventions.
- **init:** a `Directory.Build.props` above the git repository is no longer imported into a new one in the repository.
- **Report texts:** the line-endings note no longer says IDE0055 is lowered where it's off (CSharpier); a second
  `bro migrate --write` says suppressions were already carried over; the note about suppressions of whitespace
  rules says when it matters.
- **Preview:** StyleBro's `GlobalPackageReference` goes next to StyleCop's instead of into a new `ItemGroup` at the end.
