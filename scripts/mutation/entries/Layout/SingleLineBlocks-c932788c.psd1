# Trial 22: a one-line block with a comment after its } stays.
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '            || HasTrailingComment(braces.Close))'
    Replace = '            )'
    Tests = 'SingleLineBlocksTests.BlocksWithACommentAfterTheirBrace'
}
