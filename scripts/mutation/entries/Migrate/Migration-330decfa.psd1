# Trial 22 (kafkaflow): a clear majority of the code's spacing writes the option
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '&& c.Sum() >= Conventions.MinimumAgreeing &&'
    Replace = '&& c.Sum() >= 99 &&'
    Tests = 'MigrationTests.Migrate_SpacingRulesOff_KeepTheCodesSpacing_OrNameWhatFormatChanges'
}
