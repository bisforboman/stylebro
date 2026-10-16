# BRO1509: the expansion adds BRO1505's blank lines only when BRO1505 is on (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'node is BlockSyntax || isOn?.Invoke(DiagnosticIds.ElementsSeparatedByBlankLine) == false'
    Replace = 'node is BlockSyntax'
    Tests = 'SingleLineBlocksTests.OneLineType_WithoutBro1505'
}
