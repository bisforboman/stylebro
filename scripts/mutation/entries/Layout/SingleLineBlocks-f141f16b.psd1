# BRO1509: in an expanded one-line type, properties whose accessor lists get expanded too are separated
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '&& (BecomesMultiLine(items[i - 1], text, options, isOn) || BecomesMultiLine(items[i], text, options, isOn))'
    Replace = '&& false'
    Tests = 'FixOrderTests.OneLineProperties'
}
