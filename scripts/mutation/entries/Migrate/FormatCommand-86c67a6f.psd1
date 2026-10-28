# A check of 'check --json' keeps its run's report
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var checkBefore = verify && !options.Contains("--verify-no-changes");'
    Replace = 'var checkBefore = verify;'
    Tests = 'MigrationTests.Format_WithJson_ChecksFirst_TheCheckWritingNoReport_AndVerifyAnswersFromTheRecord'
}
