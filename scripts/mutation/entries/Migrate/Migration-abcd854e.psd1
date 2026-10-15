# Trial 22 (kafkaflow): only StyleCop spacing rules that are off decide SDK spacing options
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'var relevant = aspects.Where(a => a.Rules.Any(r => !setup.IsOn(r))'
    Replace = 'var relevant = aspects.Where(a => true'
    Tests = 'MigrationTests.Migrate_SpacingRulesOff_KeepTheCodesSpacing_OrNameWhatFormatChanges'
}
