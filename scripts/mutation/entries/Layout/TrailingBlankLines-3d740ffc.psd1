# Trial 21: BRO1506 leaves standalone comments (owner's choice)
@{
    File = 'src/StyleBro.Analyzers/Layout/TrailingBlankLines.cs'
    Find = 'while (above >= 0 && IsRegion(above));'
    Replace = 'while (false);'
    Tests = 'TrailingBlankLinesTests.StandaloneComments'
}
