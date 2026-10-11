# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'node.Parent is not BlockSyntax block'
    Replace = 'node.FirstAncestorOrSelf<BlockSyntax>() is not { } block'
    Tests = 'ElseAfterJumpTests'
}
