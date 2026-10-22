# Performance: BRO1617-BRO1619 read the documentation comments from the shared trivia array
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationStyleAnalyzer.cs'
    Find = '(!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))'
    Replace = '!trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)'
    Tests = 'DocumentationStyleTests'
}
