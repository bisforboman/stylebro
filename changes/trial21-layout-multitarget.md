### Changed

- **BRO1506** no longer reports a standalone comment, one with a blank line right above and right below it (a note on
  the code above, a section header): removing the blank line glued it onto unrelated code. The new
  `stylebro_blank_line_below_standalone_comments = remove` reports it again, like StyleCop's SA1512, and
  `stylebro-migrate` writes it where SA1512 is on.
- **BRO1110** with `stylebro_closing_parenthesis_placement = own_line` leaves a `)` that is already below the last item
  where it is, at any indentation, instead of reindenting it or pulling it up to a `}`.
- **BRO1149** without BRO1520 (for example with CSharpier) puts the merged `&&` where
  `dotnet_style_operator_placement_when_wrapping` says (the beginning of the line by default), unless the condition
  already wraps after its operators.

### Fixed

- **BRO1151** no longer passes elements to a `params ReadOnlySpan<T>` overload in a project that also targets a
  framework before .NET 9, where that overload doesn't exist and the build broke.
- **BRO1120** keeps a lone `//` in an otherwise empty block (an empty `catch`), which marks it as empty on purpose.
- **BRO1508/BRO1509** with `csharp_preserve_single_line_statements = false` and BRO1514 off add the braces BRO1515 and
  BRO1516 want for the statements the expansion makes multi-line, so `dotnet format` needs one run, not two.
