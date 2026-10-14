# else after an embedded block's } follows csharp_new_line_before_else
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'when (embedded is BlockSyntax && !IsClauseKeyword(@else.ElseKeyword, options)) || Break('
    Replace = 'when Break('
    Tests = 'SingleLineBlocksTests.EmbeddedBlocks'
}
