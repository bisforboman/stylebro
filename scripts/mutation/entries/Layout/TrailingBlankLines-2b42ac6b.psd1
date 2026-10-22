# Performance: BRO1506 checks the line below the comment first
@{
    File = 'src/StyleBro.Analyzers/Layout/TrailingBlankLines.cs'
    Find = 'var number = text.Lines.GetLineFromPosition(comment.SpanStart).LineNumber + 1;'
    Replace = 'var number = text.Lines.GetLineFromPosition(comment.SpanStart).LineNumber + 2;'
    Tests = 'TrailingBlankLinesTests'
}
