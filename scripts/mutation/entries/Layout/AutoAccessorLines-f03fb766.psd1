# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = '&& Line(text, initializer.SpanStart) != Line(text, close.SpanStart)'
    Replace = '&& false'
    Tests = 'AutoAccessorLinesTests'
}
