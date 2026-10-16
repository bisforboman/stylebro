# Only files that exist are left alone (missing ones are said elsewhere).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(f => File.Exists(f) && !f.StartsWith(top, StringComparison.OrdinalIgnoreCase) && !f.StartsWith(nuget'
    Replace = '.Where(f => !f.StartsWith(top, StringComparison.OrdinalIgnoreCase) && !f.StartsWith(nuget'
    Tests = 'MigrationTests.Format_LeavesFilesOutsideTheRepositoryAlone'
}
