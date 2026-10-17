# Trial 27: a misplaced summary is BRO1602's, not laid out.
@{
    File = 'src/StyleBro.Analyzers/Documentation/SummaryLayout.cs'
    Find = ' || DocumentationComments.IsMisplaced(trivia))'
    Replace = ')'
    Tests = 'FixOrderTests.MisplacedSummary_IsLeftToBRO1602'
}
