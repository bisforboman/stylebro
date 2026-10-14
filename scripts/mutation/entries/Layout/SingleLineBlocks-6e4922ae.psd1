# An embedded block's { follows csharp_new_line_before_open_brace
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'return !NewLineBeforeBrace(block, options) || Break('
    Replace = 'return Break('
    Tests = 'SingleLineBlocksTests.EmbeddedBlocks'
}
