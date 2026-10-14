# Trial 21: the expansion judges BRO1515 with its statement lines in place
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'Braces.AddToExpansion(block, text, changes, lines, options, isOn);'
    Replace = 'Braces.AddToExpansion(block, text, changes, new List<TextChange>(), options, isOn);'
    Tests = 'FixOrderTests.NestedEmbeddedStatements_InAnExpandedBody_WithoutBro1514'
}
