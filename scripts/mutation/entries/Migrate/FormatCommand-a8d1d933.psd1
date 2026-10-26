# Trial 56: DynamicData, a property function error (MSB4186) of a query without a target is retried with one
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| output.Contains("error MSB4186", StringComparison.Ordinal))'
    Replace = ')'
    Tests = 'MigrationTests.WithEmptyTarget_RetriesAPropertyFunctionErrorOfAQueryWithoutATarget'
}
