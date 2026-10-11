# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'nested => EndsInJump(nested.Statement) && EndsInJump(inner.Statement),'
    Replace = 'nested => EndsInJump(nested.Statement),'
    Tests = 'ElseAfterJumpTests'
}
