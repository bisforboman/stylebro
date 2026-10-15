# Trial 22: BRO1518 removes blank lines below a trailing comment.
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLineRuns.cs'
    Find = '            && text.ToString(TextSpan.FromBounds(text.Lines.GetLineFromPosition(stop.SpanStart).Start, stop.SpanStart)).Trim().Length == 0)'
    Replace = '            )'
    Tests = 'FixOrderTests.Braces_BlankLinesBelowATrailingComment_AreRemoved'
}
