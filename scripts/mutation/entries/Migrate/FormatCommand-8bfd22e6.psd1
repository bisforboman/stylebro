# A package's files (NuGet's folder) aren't the repository's to leave alone.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ' && !f.StartsWith(nuget, StringComparison.OrdinalIgnoreCase))'
    Replace = ')'
    Tests = 'MigrationTests.Format_LeavesFilesOutsideTheRepositoryAlone'
}
