# Performance: BRO1604/BRO1605 stop at a property without a documentation comment
@{
    File = 'src/StyleBro.Analyzers/Documentation/PropertySummaries.cs'
    Find = 't.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)))'
    Replace = 't.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)))'
    Tests = 'DocumentationTests.PropertySummaries_MatchTheAccessors'
}
