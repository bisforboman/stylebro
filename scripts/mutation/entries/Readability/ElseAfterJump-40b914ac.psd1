# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'if (!OnlyWhitespace(elseClause.ElseKeyword.GetPreviousToken(), body.First.GetFirstToken())'
    Replace = 'if (false'
    Tests = 'ElseAfterJumpTests'
}
