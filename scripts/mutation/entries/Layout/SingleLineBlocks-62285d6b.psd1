# BRO1509: a switch with a nested statement is left alone
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 's is BlockSyntax || s.DescendantNodes().OfType<StatementSyntax>().Any()'
    Replace = 's is BlockSyntax'
    Tests = 'SingleLineBlocksTests.Switch_GetsItsLines_WhenSingleLineStatementsArentPreserved'
}
