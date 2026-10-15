# BRO1504's places are counted
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'Add(CommentAboveKey, true);'
    Replace = 'Add(CommentAboveKey, false);'
    Tests = 'MigrationTests.Conventions_CountDocPeriodsCommentsLangwords'
}
