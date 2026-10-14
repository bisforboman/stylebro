# BRO1509: a using in a using stays at its level, like the formatter (trial 20)
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'statement is UsingStatementSyntax && body is UsingStatementSyntax ? indent : indent + unit'
    Replace = 'indent + unit'
    Tests = 'FixOrderTests.NestedEmbeddedStatements_InAnExpandedBody'
}
