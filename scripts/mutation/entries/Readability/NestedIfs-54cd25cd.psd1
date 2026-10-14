# Trial 21: BRO1149 without BRO1520 keeps operators at the end only where the condition has them there
@{
    File = 'src/StyleBro.Analyzers/Readability/NestedIfs.cs'
    Find = '.Any(b => text.Lines.GetLineFromPosition(b.OperatorToken.Span.End).LineNumber < text.Lines.GetLineFromPosition(b.Right.SpanStart).LineNumber))'
    Replace = '.Any(b => false))'
    Tests = 'NestedIfTests'
}
