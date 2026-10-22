# Trial 21: BRO1506 leaves standalone comments (owner's choice)
@{
    File = 'src/StyleBro.Analyzers/Layout/TrailingBlankLines.cs'
    Find = 'found.Count > 0 && !removeStandalone && IsStandalone(comment, text) ? null : found'
    Replace = 'found'
    Tests = 'TrailingBlankLinesTests.StandaloneComments'
}
