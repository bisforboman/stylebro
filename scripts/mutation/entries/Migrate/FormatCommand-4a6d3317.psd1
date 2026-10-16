# Only files that exist are left alone (missing ones are said elsewhere).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(f => !f.StartsWith(top, StringComparison.OrdinalIgnoreCase) && File.Exists(f))'
    Replace = '.Where(f => !f.StartsWith(top, StringComparison.OrdinalIgnoreCase))'
    Tests = 'MigrationTests.Format_LeavesFilesOutsideTheRepositoryAlone'
}
