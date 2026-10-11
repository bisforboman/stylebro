# BRO1616 (summary layout)
@{
    File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'
    Find = 'return textAfterStart || textBeforeEnd ?'
    Replace = 'return true ?'
    Tests = 'SummaryLayoutTests'
}
