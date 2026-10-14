# init counts only the comments BRO1506 checks
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'TrailingBlankLines.GetCheckedBlankLines(comment, text) is { } blank'
    Replace = 'TrailingBlankLines.GetBlankLinesAfterComment(comment, text) is { } blank'
    Tests = 'MigrationTests.Conventions_CountCommentsWithAndWithoutABlankLineBelow'
}
