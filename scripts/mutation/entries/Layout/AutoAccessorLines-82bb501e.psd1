# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = '&& Line(text, list.CloseBraceToken.SpanStart) == Line(text, property.Span.End)'
    Replace = ''
    Tests = 'FixOrderTests'
}
