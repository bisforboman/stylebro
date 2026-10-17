# BRO1506: a comment right after '{' is standalone (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Layout/TrailingBlankLines.cs'
    Find = ' || text.ToString(text.Lines[above].Span).TrimEnd().EndsWith("{", StringComparison.Ordinal)'
    Replace = ''
    Tests = 'TrailingBlankLinesTests.StandaloneComments_KeepTheirBlankLine'
}
