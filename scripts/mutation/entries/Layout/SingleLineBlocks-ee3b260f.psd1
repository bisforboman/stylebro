# BRO1509: BRO1526's blank line between switch sections
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '!= SwitchSectionBlankLines.Mode.Omit ? lineBreak'
    Replace = '== SwitchSectionBlankLines.Mode.Omit ? lineBreak'
    Tests = 'SingleLineBlocksTests.Switch_GetsItsLines_WhenSingleLineStatementsArentPreserved'
}
