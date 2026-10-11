# BRO1517-BRO1519 (blank lines) and the hand-offs to the other blank-line rules
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = '(gapIsReplaced && t.IsKind(SyntaxKind.EndOfLineTrivia))'
    Replace = 'false'
    Tests = 'SingleLineBlocksTests'
}
