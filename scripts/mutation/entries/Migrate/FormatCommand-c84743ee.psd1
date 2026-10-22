# Trial 42: only a condition error is retried with a target
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '&& output.Contains("error MSB4184", StringComparison.Ordinal)'
    Replace = ''
    Tests = 'MigrationTests.WithEmptyTarget_RetriesOnlyAConditionErrorOfAQueryWithoutATarget'
}
