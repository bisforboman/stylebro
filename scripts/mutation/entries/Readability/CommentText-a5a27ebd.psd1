# Trial 62: an empty comment at either end frames a group.
@{
    File = 'src/StyleBro.Analyzers/Readability/CommentText.cs'
    Find = '.Select(group => IsEmpty(group[0]) || IsEmpty(group[group.Count - 1]));'
    Replace = '.Select(group => IsEmpty(group[0]));'
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountCommentGroupsFramedByEmptyComments'
}
