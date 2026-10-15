# Trial 22 (kafkaflow): StyleCop 1.1.x's SA1413 doesn't check switch expressions
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '&& SwitchExpressionsWithoutTrailingComma(root) is > 0 and var switches)'
    Replace = '&& SwitchExpressionsWithoutTrailingComma(root) is >= 0 and var switches)'
    Tests = 'MigrationTests.Migrate_StyleCop11_KeepsTrailingCommas_WhenTheSwitchesHaveThem'
}
