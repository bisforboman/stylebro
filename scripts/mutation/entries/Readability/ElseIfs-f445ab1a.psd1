# Trial 41: the join adds the blank line BRO1519 wants after the brace it removes.
@{
    File = 'src/StyleBro.Analyzers/Readability/ElseIfs.cs'
    Find = 'blankLines.Add(new TextChange(new TextSpan(next.FullSpan.Start, 0), SingleLineBlocks.LineBreak(text, close.SpanStart)));'
    Replace = ''
    Tests = 'FixOrderTests.ElseIf_KeepsTheBlankLine'
}
