# BRO1517-BRO1519 (blank lines) and the hand-offs to the other blank-line rules
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = 'trivia[start - 2].IsKind(SyntaxKind.SingleLineCommentTrivia) && isOn(DiagnosticIds.BlankLineAfterComment)'
    Replace = 'false'
    Tests = 'BlankLineRunsTests'
}
