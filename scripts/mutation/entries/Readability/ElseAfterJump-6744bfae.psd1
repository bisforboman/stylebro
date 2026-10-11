# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'YieldStatementSyntax yield => yield.IsKind(SyntaxKind.YieldBreakStatement),'
    Replace = 'YieldStatementSyntax => true,'
    Tests = 'ElseAfterJumpTests'
}
