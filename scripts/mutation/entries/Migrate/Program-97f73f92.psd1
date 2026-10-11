# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (central.Count > 0)'
    Replace = 'if (true)'
    Tests = 'MigrationTests.PackageHints_NameCentralPackageManagementOnlyWhenTheRepositoryUsesIt'
}
