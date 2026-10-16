# Trial 22/25: a run whose files an earlier run changed formats those files too
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': (false, files.Union(touched, StringComparer.OrdinalIgnoreCase)'
    Replace = ': (false, files.Union([], StringComparer.OrdinalIgnoreCase)'
    Tests = 'MigrationTests.Format_RunsAgainOnlyWhatItsOwnCheckFound'
}
