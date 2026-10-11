# BRO1517-BRO1519 (blank lines) and the hand-offs to the other blank-line rules
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = '.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) < 2)'
    Replace = '.Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) < 0)'
    Tests = 'BlankLineRunsTests'
}
