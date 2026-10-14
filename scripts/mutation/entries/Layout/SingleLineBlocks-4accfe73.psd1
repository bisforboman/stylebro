# Where the brace rules add braces, the gap is theirs
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = '|| changes.Any(c => c.Span.IntersectsWith(span))'
    Replace = ''
    Tests = 'FixOrderTests.EmbeddedStatements_InAnExpandedBody'
}
