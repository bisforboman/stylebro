# Trial 28 (2026-10-17): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '&& keys[key].Equals("unset", StringComparison.OrdinalIgnoreCase)'
    Replace = '&& keys[key].Equals("never", StringComparison.OrdinalIgnoreCase)'
    Tests = 'MigrationTests.Init_TakesEndOfLineUnset_ForNoSetting'
}
