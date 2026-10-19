# Trial 33: '(' alone on its line: no column to keep.
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'
    Find = ' || text.Lines.GetLinePosition(open.SpanStart).Line != text.Lines.GetLinePosition(query.SpanStart).Line)'
    Replace = ')'
    Tests = 'ParenthesesTests.QueryClausesUnderFrom_MoveWithIt'
}
