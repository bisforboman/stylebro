# Trial 33: only clauses aligned under from move.
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'
    Find = 'if (token.SpanStart - line.Start != column'
    Replace = 'if (false'
    Tests = 'ParenthesesTests.QueryClausesUnderFrom_MoveWithIt'
}
