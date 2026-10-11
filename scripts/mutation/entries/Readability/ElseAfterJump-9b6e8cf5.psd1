# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = '|| elseClause.Statement.DescendantTrivia().Any('
    Replace = '|| false && elseClause.Statement.DescendantTrivia().Any('
    Tests = 'ElseAfterJumpTests'
}
