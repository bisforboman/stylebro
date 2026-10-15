# Trial 22: BRO1519 leaves an empty statement's ; to BRO1101.
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = 'if (next.Parent is EmptyStatementSyntax { Parent: BlockSyntax or SwitchSectionSyntax } && isOn(DiagnosticIds.EmptyStatement))'
    Replace = 'if (false)'
    Tests = 'FixOrderTests.EmptyStatementAfterABlock_LeavesNoBlankLine'
}
