# BRO1120: the file header's empty lines stay when file_header_template is set (Ical.Net)
@{
    File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'
    Find = '(skipHeader && text'
    Replace = '(false && text'
    Tests = 'CommentTextTests'
}
