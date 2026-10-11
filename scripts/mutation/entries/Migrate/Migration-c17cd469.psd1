# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '!own.Contains("end_of_line") &&'
    Replace = ''
    Tests = 'MigrationTests.Migrate_WritesTheFilesLineEnding'
}
