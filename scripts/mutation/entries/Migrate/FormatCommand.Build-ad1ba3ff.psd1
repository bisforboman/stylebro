# errors in projects the untouched build didn't compile aren't new (DapperAOT, trial 57)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Build.cs'
    Find = 'var unmatched = before is { ExitCode: not 0 } failedBefore'
    Replace = 'var unmatched = before is { ExitCode: 12345 } failedBefore'
    Tests = 'MigrationTests.BuildCheck_ErrorsInAProjectTheUntouchedBuildDidntCompile_AreNotCompared'
}
