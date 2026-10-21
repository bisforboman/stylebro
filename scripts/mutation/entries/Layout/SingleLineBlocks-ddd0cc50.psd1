# A comment after an attribute list on the header line: the node is skipped.
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '&& !Gap(list.CloseBracketToken, next, lineBreak + indent, text, changes))'
    Replace = '&& !Gap(list.CloseBracketToken, next, lineBreak + indent, text, changes) && false)'
    Tests = 'SingleLineBlocksTests.Attribute'
}
