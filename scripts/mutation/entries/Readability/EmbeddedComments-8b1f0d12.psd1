# BRO1132 (embedded comments)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = '&& !trivia.ToString().StartsWith("////", System.StringComparison.Ordinal)'
    Replace = ''
    Tests = 'EmbeddedCommentTests'
}
