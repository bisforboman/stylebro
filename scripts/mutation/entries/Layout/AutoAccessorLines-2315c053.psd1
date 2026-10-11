# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = 'var start = property.AttributeLists.Count > 0 ? property.AttributeLists.Last().FullSpan.End : property.SpanStart;'
    Replace = 'var start = property.SpanStart;'
    Tests = 'FixOrderTests'
}
