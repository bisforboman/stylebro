### Fixed

- **BRO1301/BRO1302** no longer rename query range variables and lambda parameters that end up in an expression tree
  (a query over `IQueryable`, an `Expression<Func<...>>` lambda): their names show at run time, in the expression's
  text and in query providers.
- **BRO1602** now fixes a `///` comment after code on the same line (`void X() /// note`); it was reported but left
  as it was.
- **BRO1120** leaves the file header's empty `//` lines alone when `file_header_template` is set (the SDK's IDE0073
  writes them).
- **BRO1509** separates the properties it expands from neighbouring properties with a blank line, so `dotnet format`
  no longer needs a second run for BRO1505.
- **BRO1125** writes `() => { }` on one line for an anonymous method with a one-line body, also when the SDK's
  formatter had already moved its `{ }` to the next line.
