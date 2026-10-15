@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = 'SyntaxKind.OpenBraceToken or SyntaxKind.CloseBraceToken when token.Parent is BlockSyntax block'
    Replace = 'SyntaxKind.None when token.Parent is BlockSyntax block'
    Tests = 'ElseIfTests'
}
