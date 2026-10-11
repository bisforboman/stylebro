# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'if (!text.ToString(line.Span).StartsWith(indent + unit, StringComparison.Ordinal) && '
    Replace = 'if (false && '
    Tests = 'ElseAfterJumpTests'
}
