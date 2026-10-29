# BRO1120: a header below a leading '#region' line is the header too (trial 60, log4net).
@{
    File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'
    Find = ' && !content.StartsWith("#region", System.StringComparison.Ordinal)'
    Replace = ''
    Tests = 'CommentTextTests.EmptyLinesInAHeaderBelowARegionLine_Stay'
}
