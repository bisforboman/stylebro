# Trial 18: format never says clean while a bulk severity hides rules the build reports
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ': exit is 0 or NotVerifiedExitCode ? NotCleanExitCode : exit;'
    Replace = ': exit;'
    Tests = 'MigrationTests.Format_IsNotClean_WhenABulkSeverityHidesRules'
}
