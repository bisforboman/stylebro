# BRO1001 inside regions
@{
    File = 'src/StyleBro.Analyzers/Ordering/MemberOrdering.cs'
    Find = '|| kind is SyntaxKind.SingleLineDocumentationCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia)'
    Replace = ')'
    Tests = 'MemberOrderingTests'
}
