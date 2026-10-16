# Trial 25 (2026-10-16): migrate/init
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'shortSummaries >= Severity.Suggestion'
    Replace = 'shortSummaries >= Severity.None'
    Tests = 'MigrationTests.Migrate_StyleSharpsShortSummaries'
}
