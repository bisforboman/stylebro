# Seventh trial (2026-10-11): migrate per project, file header escapes, line endings, init re-runs, preview
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'else if (noRestore && !restored)'
    Replace = 'else if (false)'
    Tests = 'MigrationTests.Preview_NoRestore_WithoutTheRestoreOutput_Stops'
}
