# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'if (elseClause.Statement.DescendantTokens().Any(t => SpansLines(text, t.Span))'
    Replace = 'if (false'
    Tests = 'ElseAfterJumpTests'
}
