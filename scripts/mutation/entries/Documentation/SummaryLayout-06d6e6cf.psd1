# BRO1616 (summary layout)
@{
    File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'
    Find = '&& !summary.Content.SelectMany(c => c.DescendantNodesAndSelf()).Any(IsBlockElement)'
    Replace = ''
    Tests = 'SummaryLayoutTests'
}
