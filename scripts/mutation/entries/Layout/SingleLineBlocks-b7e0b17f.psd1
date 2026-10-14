# Nested embedded statements stay (the formatter does not indent them)
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '(moved is not null && moved.DescendantNodes().OfType<StatementSyntax>().Any())'
    Replace = '(false)'
    Tests = 'SingleLineBlocksTests.EmbeddedStatements'
}
