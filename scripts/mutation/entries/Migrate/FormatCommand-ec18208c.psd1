# Trial 23: <StyleBroBaseline>none turns the kept findings' baseline off
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'value.Equals("none", StringComparison.OrdinalIgnoreCase) || '
    Replace = ''
    Tests = 'MigrationTests.Format_BaselinesTheFindingsItKeeps'
}
