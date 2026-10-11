# init after the sixth trial (2026-10-11): line endings, using order, overloads, S3260, the braces verdict
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'keys.All(k => k is not null) && keys.Any(k => k!.Value.CompareTo(keys[0]!.Value) != 0)'
    Replace = 'keys.All(k => k is not null)'
    Tests = 'MigrationTests.Conventions_CountOverloadsBro1001CouldSplit'
}
