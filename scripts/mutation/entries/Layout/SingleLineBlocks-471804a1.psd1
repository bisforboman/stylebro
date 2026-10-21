# Trial 41: BRO1509 puts the attribute lists of a member it expands on their own lines.
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'if (!AddAttributeLines(Owner(node), baseIndent, lineBreak, text, changes))'
    Replace = 'if (false)'
    Tests = 'SingleLineBlocksTests.Attribute'
}
