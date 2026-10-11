# BRO1120 (empty comments between blank lines)
@{
    File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'
    Find = 'if (blankEnd > end)'
    Replace = 'if (false)'
    Tests = 'CommentTextTests'
}
