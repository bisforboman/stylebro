# Trial 25: only files from outside the repository
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(f => !f.StartsWith(top, StringComparison.OrdinalIgnoreCase) && File.Exists(f))'
    Replace = '.Where(File.Exists)'
    Tests = 'MigrationTests.Format_FindsTheFilesAProjectCompilesFromOutsideTheRepository'
}
