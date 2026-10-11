# BRO1134 (comments in declaration headers; shares EmbeddedComments.cs with BRO1132)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = 'openBrace.Parent is BlockSyntax or AccessorListSyntax ?'
    Replace = 'openBrace.Parent is BlockSyntax ?'
    Tests = 'DeclarationCommentTests'
}
