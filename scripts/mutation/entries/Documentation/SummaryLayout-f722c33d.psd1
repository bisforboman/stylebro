# BRO1616 (summary layout)
@{
    File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'
    Find = '&& (start.Span.End - startLine.Start) + first.Length + end.Span.Length <= maxLength'
    Replace = ''
    Tests = 'SummaryLayoutTests'
}
