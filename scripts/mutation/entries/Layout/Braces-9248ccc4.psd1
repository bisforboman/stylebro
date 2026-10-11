# BRO1514-BRO1516 (braces)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = '&& Line(text, ifStatement.IfKeyword.SpanStart) == Line(text, child.Span.End)'
    Replace = ''
    Tests = 'BracesTests'
}
