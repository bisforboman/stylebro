# BRO1517-BRO1519 (blank lines) and the hand-offs to the other blank-line rules
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = '&& TrailingBlankLines.GetBlankLinesAfterComment(trivia[start - 2], text).Count > 0)'
    Replace = ')'
    Tests = 'BlankLineRunsTests'
}
