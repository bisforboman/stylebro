# Trial 43: the lowest bulk severity of the .globalconfig files counts.
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'SeverityRank(value) < SeverityRank(other)'
    Replace = 'SeverityRank(value) > SeverityRank(other)'
    Tests = 'MigrationTests.WithGlobalBulk'
}
