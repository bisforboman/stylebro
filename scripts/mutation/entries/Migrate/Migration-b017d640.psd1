# Trial 22 (kafkaflow): only StyleCop 1.1.x skips switch expressions
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'setup.Version?.StartsWith("1.1.", StringComparison.Ordinal) == true'
    Replace = 'true'
    Tests = 'MigrationTests.Migrate_StyleCop11_LeavesSwitchExpressionsWithoutTrailingCommas'
}
