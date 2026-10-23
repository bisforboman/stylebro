# Only a root file with inits block can show the copying is off (trial 46)
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = 'before is not null && before.Contains(BeginMarker, StringComparison.Ordinal);'
    Replace = 'before is not null;'
    Tests = 'MigrationTests.CanProveCopyOff'
}
