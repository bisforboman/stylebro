# Trial 22 (kafkaflow): only switch expressions without the trailing comma count
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 's.Arms.Count > 0 && s.Arms.SeparatorCount < s.Arms.Count'
    Replace = 's.Arms.Count > 0'
    Tests = 'MigrationTests.Migrate_StyleCop11_KeepsTrailingCommas_WhenTheSwitchesHaveThem'
}
