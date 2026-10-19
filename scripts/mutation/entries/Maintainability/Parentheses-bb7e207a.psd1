# Trial 33: only clauses that start their line.
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'
    Find = '|| text.ToString(TextSpan.FromBounds(line.Start, token.SpanStart)).Any(c => c != '' ''))'
    Replace = ')'
    Tests = 'ParenthesesTests.QueryClausesUnderFrom_MoveWithIt'
}
