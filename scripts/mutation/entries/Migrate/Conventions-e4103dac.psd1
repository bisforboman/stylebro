# init: 'Getsome text' doesn't start the StyleCop way (#132)
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '!starts.Any(s => words == s || words.StartsWith(s + " ", StringComparison.Ordinal))'
    Replace = '!starts.Any(s => words.StartsWith(s, StringComparison.Ordinal))'
    Tests = 'MigrationTests.Conventions_CountPropertyAndConstructorSummaries_TheStyleCopWayOrTheirOwn'
}
