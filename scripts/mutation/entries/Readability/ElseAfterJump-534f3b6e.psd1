# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'if (ifLine.Start + indent.Length != node.SpanStart)'
    Replace = 'if (false)'
    Tests = 'ElseAfterJumpTests'
}
