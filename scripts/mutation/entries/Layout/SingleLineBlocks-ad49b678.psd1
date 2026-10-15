@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'else if (HasTrailingComment(braces.Close) && '
    Replace = 'else if (false && '
    Tests = 'SingleLineBlocksTests.BlocksWithACommentAfterTheirBrace'
}
