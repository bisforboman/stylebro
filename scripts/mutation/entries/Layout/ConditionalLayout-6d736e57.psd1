# BRO1524 (split conditional expressions)
@{
    File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'
    Find = 'if (before || after || Line(text, part.SpanStart) != Line(text, part.Span.End))'
    Replace = 'if (before || after)'
    Tests = 'ConditionalLayoutTests'
}
