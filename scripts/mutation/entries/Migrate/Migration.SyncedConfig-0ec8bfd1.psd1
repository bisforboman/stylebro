# Same lines only (trial 44)
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = '.SequenceEqual(own, StringComparer.Ordinal)'
    Replace = '.Count > 0'
    Tests = 'MigrationTests.PackageCopy'
}
