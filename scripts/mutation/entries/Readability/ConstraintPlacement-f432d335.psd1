# Fix order: BRO1107/BRO1108 judge a list with BRO1116's joins made
@{
    File = 'src/StyleBro.Analyzers/Readability/ConstraintPlacement.cs'
    Find = 'length += clause.WhereKeyword.SpanStart - before.Span.End + clause.Span.Length;'
    Replace = 'length += 0;'
    Tests = 'ConstraintPlacementTests'
}
