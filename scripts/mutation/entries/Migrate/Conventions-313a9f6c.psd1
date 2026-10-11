# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'var system = sort != 0 || apart < MinimumAgreeing ? 0 : Winner(c[0], c[1]);'
    Replace = 'var system = 0;'
    Tests = 'MigrationTests.Conventions_DecideTheUsingOrder'
}
