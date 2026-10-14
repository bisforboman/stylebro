# baseline reads each pass on its own
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = 'whitespaceExclusions is null ? ["whitespace", "style", "analyzers"]'
    Replace = 'whitespaceExclusions is null ? [string.Empty]'
    Tests = 'MigrationTests.Baseline_RunsEachPassOnItsOwn'
}
