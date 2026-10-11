# BRO1149 (nested ifs)
# Sonar ids (NodeCodeFixProvider): no action where the shared logic skips
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '|| !Trivia.IsBlank(outer, TextSpan.FromBounds(inner.Span.End, outer.Span.End))'
    Replace = ''
    Tests = 'NestedIfTests.Skipped'
}
