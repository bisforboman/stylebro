### Fixed

- BRO1608-BRO1614 and BRO1616 leave documentation with malformed XML (a bare `<` as in `when a < b`) alone too, like the
  other documentation rules, instead of removing, unwrapping, renaming or reordering its tags. An empty `name=""` no
  longer counts as malformed.
- Expanding a one-line method body (BRO1508/BRO1509) with `csharp_preserve_single_line_statements = false` now also lays
  out a braced `if` with an unbraced `else` (`if (x) { a; } else b;`) and other braced loop bodies the way the SDK's
  formatting does, so `format` no longer needs another run for them.
