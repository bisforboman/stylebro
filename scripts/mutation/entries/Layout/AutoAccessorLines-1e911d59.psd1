# A header over several lines: IDE0055 wants the brace on its own line (StabilityMatrix loop)
@{
    File = 'src/StyleBro.Analyzers/Layout/AutoAccessorLines.cs'
    Find = '            || Line(text, start.SpanStart) != Line(text, before.Span.End)'
    Replace = ''
    Tests = 'AutoAccessorLinesTests'
}
