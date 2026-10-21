# Only lists sharing a line with the next token: a blank line below an attribute stays (BRO1525).
@{
    File = 'src/StyleBro.Analyzers/Layout/SingleLineBlocks.cs'
    Find = 'if (Line(text, list.CloseBracketToken.SpanStart) == Line(text, next.SpanStart)'
    Replace = 'if (true'
    Tests = 'SingleLineBlocksTests.Attribute'
}
