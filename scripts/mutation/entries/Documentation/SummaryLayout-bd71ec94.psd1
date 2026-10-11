# BRO1616 (summary layout)
@{
    File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'
    Find = 'if (end.Name.LocalName.ValueText != "summary")'
    Replace = 'if (false)'
    Tests = 'SummaryLayoutTests'
}
