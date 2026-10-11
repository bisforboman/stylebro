# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'block.Statements.Count > 0 && EndsInJump(block.Statements.Last()),'
    Replace = 'block.Statements.Count > 0,'
    Tests = 'ElseAfterJumpTests'
}
