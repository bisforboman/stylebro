# Only packages count (trial 44)
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = 'type.GetString() != "package"'
    Replace = 'type.GetString() == "none"'
    Tests = 'MigrationTests.PackageCopy_OnlyCountsPackages'
}
