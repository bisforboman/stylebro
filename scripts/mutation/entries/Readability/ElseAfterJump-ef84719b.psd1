# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = '|| node.ContainsDiagnostics'
    Replace = ''
    Tests = 'ElseAfterJumpTests'
}
