# BRO1616 (summary layout)
@{
    File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'
    Find = 'if (prefix.Trim() != "///" || '
    Replace = 'if ('
    Tests = 'SummaryLayoutTests'
}
