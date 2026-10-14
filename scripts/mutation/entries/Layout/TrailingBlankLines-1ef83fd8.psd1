# Trial 21: BRO1506 leaves standalone comments (owner's choice)
@{
    File = 'src/StyleBro.Analyzers/Layout/TrailingBlankLines.cs'
    Find = '(!removeStandalone && IsStandalone'
    Replace = '(IsStandalone'
    Tests = 'TrailingBlankLinesTests.StandaloneComments'
}
