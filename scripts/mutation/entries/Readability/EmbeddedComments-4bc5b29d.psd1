# BRO1134 (comments in declaration headers; shares EmbeddedComments.cs with BRO1132)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = 'while (token.Parent is AttributeListSyntax list && list.Parent == owner)'
    Replace = 'while (token.Parent is AttributeListSyntax list && list.Parent == null)'
    Tests = 'DeclarationCommentTests'
}
