# Comments with the blank line BRO1504 wants are counted
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'counts[CommentAboveKey][0] += spaced.Count('
    Replace = 'counts[CommentAboveKey][0] += 0 * spaced.Count('
    Tests = 'MigrationTests.Conventions_CountDocPeriodsCommentsLangwords'
}
