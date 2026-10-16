# Only files that exist are left alone (missing ones are said elsewhere).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(f => File.Exists(f) && !f.StartsWith(top, StringComparison.OrdinalIgnoreCase))'
    Replace = '.Where(f => !f.StartsWith(top, StringComparison.OrdinalIgnoreCase))'
    Tests = 'MigrationTests.Format_LeavesFilesOutsideTheRepositoryAlone'
}
