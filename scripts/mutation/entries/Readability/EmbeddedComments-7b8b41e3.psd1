# BRO1132 (embedded comments)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = 'next.IsKind(SyntaxKind.CloseBraceToken)'
    Replace = 'false'
    Tests = 'EmbeddedCommentTests'
}
