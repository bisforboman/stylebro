# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = ' || Line(text, a.SpanStart) != Line(text, a.Span.End))'
    Replace = ')'
    Tests = 'AutoAccessorLinesTests'
}
