# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = '&& text.Lines.GetLineFromPosition(branch.OpenBraceToken.SpanStart).LineNumber != text.Lines.GetLineFromPosition(branch.CloseBraceToken.SpanStart).LineNumber'
    Replace = ''
    Tests = 'ElseAfterJumpTests'
}
