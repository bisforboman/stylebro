# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = '|| !EndsInJump(node.Statement)'
    Replace = ''
    Tests = 'ElseAfterJumpTests'
}
