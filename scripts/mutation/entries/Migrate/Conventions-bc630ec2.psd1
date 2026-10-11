# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'convention.Off is { } offLine && RuleOf(offLine) is not null &&'
    Replace = 'convention.Off is { } offLine &&'
    Tests = 'MigrationTests.Conventions_BracesWithoutAMajority'
}
