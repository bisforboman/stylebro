# BRO1509 without BRO1505: no blank line after a '}' that stays on its line (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '&& (node is BlockSyntax || EndsOnItsOwnLine(previous, text, options, isOn))'
    Replace = ''
    Tests = 'SingleLineBlocksTests.OneLineType_WithoutBro1505_NoBlankLine'
}
