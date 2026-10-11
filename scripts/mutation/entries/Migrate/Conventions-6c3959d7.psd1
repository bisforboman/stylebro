# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '&& !info.Contains("eol=", StringComparison.Ordinal)'
    Replace = ''
    Tests = 'MigrationTests.Conventions_Converted_'
}
