# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = '|| (elseClause.Statement is BlockSyntax closing && !OnlyWhitespace('
    Replace = '|| (elseClause.Statement is BlockSyntax closing && false && !OnlyWhitespace('
    Tests = 'ElseAfterJumpTests'
}
