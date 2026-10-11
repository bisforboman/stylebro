# BRO1134 (comments in declaration headers; shares EmbeddedComments.cs with BRO1132)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& !(previous.IsKind(SyntaxKind.OpenBracketToken) && previous.Parent.IsKind(SyntaxKind.CollectionExpression))'
    Replace = ''
    Tests = 'BlankLineAfterTests'
}
