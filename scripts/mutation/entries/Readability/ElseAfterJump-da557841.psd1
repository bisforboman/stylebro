# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'if (lastLine != firstLine.LineNumber)'
    Replace = 'if (false)'
    Tests = 'ElseAfterJumpTests'
}
