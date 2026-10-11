# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = 'if (Line(text, before.Span.End) == Line(text, close.SpanStart)'
    Replace = 'if (false'
    Tests = 'AutoAccessorLinesTests'
}
