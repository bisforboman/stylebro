# The check before a fixing run writes no report (format --json checks first)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'options.RemoveRange(at, 2);'
    Replace = 'options[at + 1] = reportFolder;'
    Tests = 'MigrationTests.Format_WithJson_ChecksFirst_TheCheckWritingNoReport_AndVerifyAnswersFromTheRecord'
}
