# Format: a run's files keep their path between runs (the host's workspace key)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '{prefix}{Environment.ProcessId}-'
    Replace = '{prefix}{Guid.NewGuid():N}-'
    Tests = 'MigrationTests.ARunsFiles_AreNamedByTheProcessAndTheirContent'
}
