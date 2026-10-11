# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = '&& Line(text, start) == Line(text, list.OpenBraceToken.GetPreviousToken().Span.End)'
    Replace = ''
    Tests = 'FixOrderTests'
}
