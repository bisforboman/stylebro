# BRO1503 convention: a brace with a comment after it isn't counted (trial 61)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '|| open.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))'
    Replace = ''
    Tests = 'MigrationTests.Conventions_CountNamespaceAndTypeBracesWithAndWithoutABlankLineBelow'
}
