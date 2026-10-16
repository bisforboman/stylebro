# Trial 22/25: a run whose files an earlier run changed formats those files too
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return (false, files.Union(touched, StringComparer.OrdinalIgnoreCase)'
    Replace = 'return (false, files.Union([], StringComparer.OrdinalIgnoreCase)'
    Tests = 'MigrationTests.Format_RunsAgainOnlyWhatItsOwnCheckFound'
}
