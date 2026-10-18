# Trial 31: 'Lib(netstandard1.3)' is Lib's skip for netstandard1.3 (Hangfire)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            ? (match.Groups["name"].Value, match.Groups["framework"].Value)
            : (skipped, run);'
    Replace = '            ? (skipped, run)
            : (skipped, run);'
    Tests = 'MigrationTests.ASkipOfOneFramework'
}
