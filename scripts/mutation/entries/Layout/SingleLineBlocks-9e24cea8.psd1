# BRO1509: a switch with a block in a section is left alone
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 's is BlockSyntax || s.DescendantNodes()'
    Replace = 's.DescendantNodes()'
    Tests = 'SingleLineBlocksTests.Switch_GetsItsLines_WhenSingleLineStatementsArentPreserved'
}
