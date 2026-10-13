# Trial 18: format never says clean while a bulk severity hides rules the build reports
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'exit = exit == 0 ? NotCleanExitCode : exit;'
    Replace = 'exit = exit;'
    Tests = 'MigrationTests.Format_IsNotClean_WhenABulkSeverityHidesRules'
}
