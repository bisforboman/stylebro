# A do-while with a block keeps } while together
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'case DoStatementSyntax @do when embedded is not BlockSyntax:'
    Replace = 'case DoStatementSyntax @do:'
    Tests = 'SingleLineBlocksTests.EmbeddedBlocks'
}
