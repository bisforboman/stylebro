### Changed

- **BRO1504** no longer asks for a blank line above a `//` comment between the items of a list (arguments, parameters,
  attribute arguments, initializers, collection expressions, tuples): the comment describes the next item, and the
  blank line split the list. StyleCop's SA1515 asks for it; this only makes StyleBro quieter.

### Fixed

- **BRO1101** removing a `;` after a block (`};`, which the SDK's formatter splits onto its own line) followed by a blank
  line or a comment no longer leaves two blank lines for a second `dotnet format` run.
- **BRO1122** removes the parentheses the new literal leaves unnecessary when BRO1405 is on:
  `((double)0).ToString()` becomes `0D.ToString()` in one run instead of `(0D).ToString()` first.
