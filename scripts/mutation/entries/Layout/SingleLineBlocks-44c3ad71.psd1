# BRO1509 splits embedded statements only with csharp_preserve_single_line_statements = false
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'items.Count > 0 && !PreservesSingleLineStatements(options)'
    Replace = 'items.Count > 0'
    Tests = 'SingleLineBlocksTests.EmbeddedStatements'
}
