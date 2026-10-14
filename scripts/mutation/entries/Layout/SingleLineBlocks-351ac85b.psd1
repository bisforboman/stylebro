# BRO1509: nested embedded statements go one level deeper each (trial 20)
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'AddEmbeddedStatementLines(body, bodyIndent, unit, lineBreak, text, options, changes);'
    Replace = ''
    Tests = 'SingleLineBlocksTests.EmbeddedStatements_GetTheirOwnLines_WhenSingleLineStatementsArentPreserved'
}
