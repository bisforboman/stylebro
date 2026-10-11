# BRO1143 (no 'else' after a jump)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseAfterJump.cs'
    Find = 'if (statements.Any(s => s is LocalDeclarationStatementSyntax { UsingKeyword.RawKind: not 0 }))'
    Replace = 'if (false)'
    Tests = 'ElseAfterJumpTests'
}
