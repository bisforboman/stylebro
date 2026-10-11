# BRO1107/BRO1108: lists with a multi-line string are left alone
@{
    File = 'src/StyleBro.Analyzers/Readability/ParameterLayout.cs'
    Find = '            || items.Any(i => Line(text, i.SpanStart) != Line(text, i.Span.End)
                && i.DescendantTokens().Any(t => Line(text, t.SpanStart) != Line(text, t.Span.End))))'
    Replace = ')'
    Tests = 'ParameterLayoutTests.ListsWithAMultiLineString'
}
