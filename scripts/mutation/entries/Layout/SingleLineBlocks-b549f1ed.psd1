# BRO1509 without BRO1505: a body BRO1509 expands gets BRO1519's blank line after it (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '|| (GetBraces(owner) is not null && isOn(DiagnosticIds.SingleLineElement) && IsReported(owner, text, options, isOn))'
    Replace = ''
    Tests = 'SingleLineBlocksTests.OneLineType_WithoutBro1505_GetsOnlyBro1519sBlankLines'
}
