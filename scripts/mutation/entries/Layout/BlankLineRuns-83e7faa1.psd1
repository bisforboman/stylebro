# Trial 21: BRO1517 takes the blank lines below a standalone comment BRO1506 leaves
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = 'text, removeStandaloneComments).Count > 0)'
    Replace = 'text, true).Count > 0)'
    Tests = 'BlankLineRunsTests.BlankLinesBelowAStandaloneComment'
}
