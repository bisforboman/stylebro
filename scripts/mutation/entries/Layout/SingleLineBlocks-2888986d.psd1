# A comment in the gap: the embedded statement stays
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '|| !before.TrailingTrivia.Concat(after.LeadingTrivia).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia))'
    Replace = ''
    Tests = 'SingleLineBlocksTests.EmbeddedStatements'
}
