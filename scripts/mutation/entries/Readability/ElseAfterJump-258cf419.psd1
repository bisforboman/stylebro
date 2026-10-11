# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = '|| node.Else is not { Statement: not IfStatementSyntax } elseClause'
    Replace = '|| node.Else is not { } elseClause'
    Tests = 'ElseAfterJumpTests'
}
