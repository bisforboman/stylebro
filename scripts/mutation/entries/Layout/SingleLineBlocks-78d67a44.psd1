# BRO1509: a switch's brace follows control_blocks
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'BlockSyntax or SwitchStatementSyntax => "control_blocks",'
    Replace = 'BlockSyntax => "control_blocks",'
    Tests = 'FixOrderTests.Switch_InAnExpandedBody'
}
