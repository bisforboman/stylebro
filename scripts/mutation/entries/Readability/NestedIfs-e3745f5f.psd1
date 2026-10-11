# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '|| !Trivia.IsBlank(outer, TextSpan.FromBounds(outer.Condition.Span.End, inner.Condition.SpanStart))'
    Replace = ''
    Tests = 'NestedIfTests.Skipped'
}
