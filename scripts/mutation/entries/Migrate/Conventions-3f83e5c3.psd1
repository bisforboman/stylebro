# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'group[^1].Index - group[0].Index == group.Count - 1'
    Replace = 'true'
    Tests = 'MigrationTests.Conventions_CountOverloadsBro1001CouldSplit'
}
