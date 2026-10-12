# BRO1509: an expanded property gets BRO1505's blank lines next to neighbouring properties (actions/runner)
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '&& isOn(DiagnosticIds.ElementsSeparatedByBlankLine))'
    Replace = '&& false)'
    Tests = 'FixOrderTests.OneLineProperties'
}
