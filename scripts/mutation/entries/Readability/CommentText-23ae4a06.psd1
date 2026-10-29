# Trial 62: the file header and a '//' braces hold don't count.
@{
    File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'
    Find = '.Where(group => !IsFileHeader(group[0].SpanStart, text) && !MarksEmptyBraces(group) && !group.All(IsEmpty))'
    Replace = '.Where(group => !group.All(IsEmpty))'
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountCommentGroupsFramedByEmptyComments'
}
