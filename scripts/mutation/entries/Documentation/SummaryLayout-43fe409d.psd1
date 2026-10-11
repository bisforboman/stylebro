# BRO1616 (summary layout)
@{
    File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'
    Find = ' || text.ToString(TextSpan.FromBounds(end.Span.End, endLine.End)).Trim().Length > 0)'
    Replace = ')'
    Tests = 'SummaryLayoutTests'
}
