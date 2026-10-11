# BRO1139 (else if)
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = '&& !block.CloseBraceToken.GetNextToken().IsKind(SyntaxKind.ElseKeyword)'
    Replace = ''
    Tests = 'ElseIfTests'
}
