# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = '.Any(f => f.Child == node.Statement)'
    Replace = '.Any()'
    Tests = 'ElseAfterJumpTests'
}
