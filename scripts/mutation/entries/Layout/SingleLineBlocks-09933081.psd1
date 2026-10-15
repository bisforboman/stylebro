# BRO1508 inside a single-line block whose rule is off (trial 23, jint)
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '&& (isOn is null || !IsInsideKeptBlock(node, text, options, isOn));'
    Replace = ';'
    Tests = 'SingleLineBlocksTests.InnerBlock_StaysInsideABody_WhenBRO1509IsOff'
}
