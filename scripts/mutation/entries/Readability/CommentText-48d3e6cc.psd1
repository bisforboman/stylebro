# BRO1120 (empty comments between blank lines)
@{
    File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'
    Find = '|| !IsBlankLine(text, text.Lines[first.LineNumber - 1]))'
    Replace = ')'
    Tests = 'CommentTextTests'
}
