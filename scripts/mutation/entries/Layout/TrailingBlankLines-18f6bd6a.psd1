# Trial 21: BRO1506 leaves standalone comments (owner's choice)
@{
    File = 'src/StyleBro.Analyzers/Layout/TrailingBlankLines.cs'
    Find = 'return above >= 0 && (IsBlank(text, text.Lines[above].Span) || text.ToString(text.Lines[above].Span).TrimEnd().EndsWith("{", StringComparison.Ordinal))'
    Replace = 'return above >= 0'
    Tests = 'TrailingBlankLinesTests'
}
