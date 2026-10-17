# Trial 23: <StyleBroBaseline>none turns the kept findings' baseline off
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return (true, off || value.Contains('
    Replace = 'return (true, value.Contains('
    Tests = 'MigrationTests.Format_BaselinesTheFindingsItKeeps'
}
