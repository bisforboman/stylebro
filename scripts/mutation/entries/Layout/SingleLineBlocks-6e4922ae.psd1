# An embedded block's { follows csharp_new_line_before_open_brace
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '? !NewLineBeforeBrace(block, options) || Break('
    Replace = '? Break('
    Tests = 'SingleLineBlocksTests.EmbeddedBlocks'
}
