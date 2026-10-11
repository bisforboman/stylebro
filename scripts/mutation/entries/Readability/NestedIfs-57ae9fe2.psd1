# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '|| !Trivia.IsBlank(deepest, TextSpan.FromBounds(deepest.Condition.Span.End, deepest.CloseParenToken.SpanStart))'
    Replace = ''
    Tests = 'NestedIfTests.Skipped'
}
