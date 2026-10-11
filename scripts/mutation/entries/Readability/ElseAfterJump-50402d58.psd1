# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = '|| !block.DescendantTokens().Any('
    Replace = '|| true || !block.DescendantTokens().Any('
    Tests = 'ElseAfterJumpTests'
}
