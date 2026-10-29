# BRO1120: the file header's empty lines stay (Ical.Net; trial 60, log4net: also below a '#region' line)
@{
    File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'
    Find = '|| IsFileHeader(group[0].SpanStart, text)'
    Replace = '|| false'
    Tests = 'CommentTextTests'
}
