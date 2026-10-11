# BRO1616 (summary layout)
@{
    File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'
    Find = 'return singleLine ? null :'
    Replace = 'return false ? null :'
    Tests = 'SummaryLayoutTests'
}
