# Trial 50: a failed run outranks findings
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'exit is 0 || (exit == NotCleanExitCode && code is not (0 or NotCleanExitCode)) ? code : exit;'
    Replace = 'exit is 0 ? code : exit;'
    Tests = 'MigrationTests.Runs_AFailureAfterFindings_IsTheExitCode'
}
