# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'BlockSyntax { Statements.Count: 0 } => null,'
    Replace = ''
    Tests = 'ElseAfterJumpTests'
}
